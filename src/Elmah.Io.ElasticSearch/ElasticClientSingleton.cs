using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using Elasticsearch.Net;
//using Elasticsearch.Net.ConnectionPool;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    /// <summary>
    /// The IElasticClient is intended to be a singleton.  If we were using
    /// IOC we could inject it as such, but because the Elmah base code is
    /// so old it's not really supported.  Hence this class to manage it for us.
    /// </summary>
    public class ElasticClientSingleton : IDisposable
    {
        private const string MultiFieldSuffix = "raw";

        private static ElasticClientSingleton _instance;
        public IElasticClient Client;
        private IElasticConnectionConfiguration _connectionConfiguration = new ElasticConnectionConfiguration();

        public void Dispose()
        {
            _instance = null;
            Client = null;
            _connectionConfiguration = null;
        }

        private ElasticClientSingleton(IDictionary config)
        {
            if (config == null)
            {
                config = ReadConfig();
            }

            var connectionString = LoadConnectionString(config);
            Client = GetElasticClient(connectionString, config);
        }

        public static ElasticClientSingleton GetInstance(IDictionary config)
        {
            return _instance ?? (_instance = new ElasticClientSingleton(config));
        }

        /// <summary>
        /// Get the ElasticSearch client connection and initialize the index if necessary.
        /// </summary>
        private IElasticClient GetElasticClient(string connectionString, IDictionary config)
        {
            var esClusterConfig = _connectionConfiguration.Parse(connectionString);
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (esClusterConfig == null)
            {
                //connection string is supplied in a deprecated format
                #pragma warning disable 618
                esClusterConfig = _connectionConfiguration.BuildClusterConfigDeprecated(config, connectionString);
                #pragma warning restore 618
            }

            var connectionPool = new StaticConnectionPool(esClusterConfig.NodeUris);
            var conSettings = new ConnectionSettings(connectionPool);
            conSettings.DefaultIndex(esClusterConfig.DefaultIndex);
            //conSettings.DisableDirectStreaming();//This should only be used for debugging, it will slow things down
            
            // set basic auth if username and password are provided in config string.
            if (!string.IsNullOrWhiteSpace(esClusterConfig.Username) && !string.IsNullOrWhiteSpace(esClusterConfig.Password))
            {
                conSettings.BasicAuthentication(esClusterConfig.Username, esClusterConfig.Password);
            }

            var esClient = new ElasticClient(conSettings);
            var indexExistsResponse = esClient.IndexExists(new IndexExistsRequest(esClusterConfig.DefaultIndex)).VerifySuccessfulResponse();
            if (!indexExistsResponse.Exists)
            {
                CreateIndexWithMapping(esClient, esClusterConfig.DefaultIndex);
            }
            return esClient;
        }




        private static void CreateIndexWithMapping(IElasticClient esClient, string defaultIndex)
        {
            esClient.CreateIndex(defaultIndex).VerifySuccessfulResponse();
            esClient.Map<ErrorDocument>(m => m
                .AutoMap()
                .Properties(CreateMultiFieldsForAllStrings)
                )
                .VerifySuccessfulResponse();
        }

        private static PropertiesDescriptor<ErrorDocument> CreateMultiFieldsForAllStrings(PropertiesDescriptor<ErrorDocument> props)
        {
            var members = typeof(ErrorDocument)
                .GetProperties()
                .Where(x => x.PropertyType == typeof(string)
                    && x.Name != "Id" //id field is obviously excluded
                    && x.Name != "ErrorXml"//errorXML field is so long it has no indexer on it at all
                    );
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var m in members)
            {
                var name = m.Name;
                name = char.ToLowerInvariant(name[0]) + name.Substring(1);//lowercase the first character
                props
                    .Text(s => s                        
                        .Name(name)
                        .Fields(pprops => pprops
                            .Text(ps => ps.Name(name).Index(true))
                            .Text(ps => ps.Name(MultiFieldSuffix).Index(false))
                        )                    
                    );
            }
            return props;
        }

        //Inspired by SimpleServiceProviderFactory.CreateFromConfigSection(string sectionName)
        //https://github.com/mikefrey/ELMAH/blob/master/src/Elmah/SimpleServiceProviderFactory.cs
        //definitely a little redundant given that the same code gets passed into the actual ErrorLog impl.
        private static IDictionary ReadConfig()
        {
            //
            // Get the configuration section with the settings.
            //
            var config = (IDictionary)ConfigurationManager.GetSection("elmah/errorLog");

            if (config == null)
                return null;

            //
            // We modify the settings by removing items as we consume
            // them so make a copy here.
            //
            config = (IDictionary)((ICloneable)config).Clone();

            //
            // Remove the type specification of the service provider.
            //
            const string typeKey = "type";
            var typeString = ElasticSearchErrorLog.ResolveConfigurationParam(config, typeKey);
            if (string.IsNullOrEmpty(typeString))
            {
                return null;
            }

            config.Remove(typeKey);
            return config;
        }

        /// <summary>
        /// returns the ElasticSearch connection string
        /// </summary>
        private static string LoadConnectionString(IDictionary config)
        {
            // From ELMAH source
            // First look for a connection string name that can be
            // subsequently indexed into the <connectionStrings> section of
            // the configuration to get the actual connection string.
            var connectionStringName = (string)config["connectionStringName"];

            if (string.IsNullOrEmpty(connectionStringName))
            {
                throw new ApplicationException("You must specify the 'connectionStringName' attribute on the <errorLog /> element.");
            }
            var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (settings != null)
            {
                return settings.ConnectionString;
            }

            throw new ApplicationException(string.Format("Could not find a ConnectionString with the name '{0}'.", connectionStringName));
        }

    }
}
