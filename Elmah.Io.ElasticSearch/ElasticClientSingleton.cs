using System;
using System.Collections;
using System.Configuration;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    public class ElasticClientSingleton : IDisposable
    {
        private const string MultiFieldSuffix = "raw";

        private static ElasticClientSingleton _instance;
        public IElasticClient Client;

        private ElasticClientSingleton()
        {
            var config = ReadConfig();
            var url = LoadConnectionString(config);
            var defaultIndex = GetDefaultIndex(config, url);
            var conString = RemoveDefaultIndexFromConnectionString(url);
            var conSettings = new ConnectionSettings(new Uri(conString), defaultIndex);

            _instance.Client = new ElasticClient(conSettings);
            if (!_instance.Client.IndexExists(new IndexExistsRequest(defaultIndex)).Exists)
            {
                InitIndex(defaultIndex);
            }
        }

        public static ElasticClientSingleton Instance
        {
            get { return _instance ?? (_instance = new ElasticClientSingleton()); }
        }

        private static void InitIndex(string defaultIndex)
        {
            _instance.Client.CreateIndex(defaultIndex).VerifySuccessfulResponse();
            _instance.Client.Map<ErrorDocument>(m => m
                .MapFromAttributes()
                .Properties(props => props
                    .MultiField(mf => mf
                        .Name(n => n.ApplicationName)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.ApplicationName.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.ApplicationName).Index(FieldIndexOption.Analyzed))
                        )
                    )
                    .MultiField(mf => mf
                        .Name(n => n.Message)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.Message.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.Message).Index(FieldIndexOption.Analyzed))
                        )
                    )
                    .MultiField(mf => mf
                        .Name(n => n.Type)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.Type.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.Type).Index(FieldIndexOption.Analyzed))
                        )
                    )
                    .MultiField(mf => mf
                        .Name(n => n.Source)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.Source.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.Source).Index(FieldIndexOption.Analyzed))
                        )
                    )
                    .MultiField(mf => mf
                        .Name(n => n.CustomerName)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.CustomerName.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.CustomerName).Index(FieldIndexOption.Analyzed))
                )
            )
                    .MultiField(mf => mf
                        .Name(n => n.EnvironmentName)
                        .Fields(pprops => pprops
                            .String(ps => ps.Name(p => p.EnvironmentName.Suffix(MultiFieldSuffix)).Index(FieldIndexOption.NotAnalyzed))
                            .String(ps => ps.Name(p => p.EnvironmentName).Index(FieldIndexOption.Analyzed))
                        )
                    )
                )
            )
            .VerifySuccessfulResponse();
        }

        //Inspired by SimpleServiceProviderFactory.CreateFromConfigSection(string sectionName)
        //https://github.com/mikefrey/ELMAH/blob/master/src/Elmah/SimpleServiceProviderFactory.cs
        //definitely a little redundant givent that the same code gets passed into the actual ErrorLog impl.
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

        private static string LoadConnectionString(IDictionary config)
        {
            // From ELMAH source
            // First look for a connection string name that can be 
            // subsequently indexed into the <connectionStrings> section of 
            // the configuration to get the actual connection string.
            var connectionStringName = (string)config["connectionStringName"];

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (settings != null)
                    return settings.ConnectionString;

                throw new ApplicationException(string.Format("Could not find a ConnectionString with the name '{0}'.", connectionStringName));
            }

            throw new ApplicationException("You must specifiy the 'connectionStringName' attribute on the <errorLog /> element.");
        }

        /// <summary>
        /// In the previous version the default index would come from the elmah configuration.
        /// 
        /// This version supports pulling the default index from the connection string which is cleaner and easier to manage.
        /// </summary>
        internal static string GetDefaultIndex(IDictionary config, string connectionString)
        {
            //step 1: try to get the default index from the connection string
            var defaultConnectionString = GetDefaultIndexFromConnectionString(connectionString);
            if (!string.IsNullOrEmpty(defaultConnectionString))
            {
                return defaultConnectionString;
            }

            //step 2: we couldn't find it in the connection string so get it from the elmah config section or use the default
            return !string.IsNullOrWhiteSpace(config["defaultIndex"] as string) ? config["defaultIndex"].ToString().ToLower() : "elmah";
        }

        internal static string GetDefaultIndexFromConnectionString(string connectionString)
        {
            Uri myUri = new Uri(connectionString);

            string[] pathSegments = myUri.Segments;
            string ourIndex = string.Empty;

            if (pathSegments.Length > 1)
            {
                //We might have a index here
                ourIndex = pathSegments[1];
                ourIndex = RemoveTrailingSlash(ourIndex);
            }

            return ourIndex;
        }

        internal static string RemoveTrailingSlash(string connectionString)
        {
            if (connectionString.EndsWith("/"))
            {
                connectionString = connectionString.Substring(0, connectionString.Length - 1);
            }

            return connectionString;
        }

        internal static string RemoveDefaultIndexFromConnectionString(string connectionString)
        {
            var uri = new Uri(connectionString);
            return uri.GetLeftPart(UriPartial.Authority);
        }

        public void Dispose()
        {
            _instance = null;
            Client = null;
        }
    }

}
