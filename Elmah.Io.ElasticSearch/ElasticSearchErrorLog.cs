using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using Nest;

[assembly:InternalsVisibleTo("Elmah.Io.ElasticSearch.Tests")]
namespace Elmah.Io.ElasticSearch
{
    public class ElasticSearchErrorLog : ErrorLog
    {
        private IElasticClient _elasticClient;

        public ElasticSearchErrorLog(IDictionary config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            InitElasticSearch(config);
            ApplicationName = ResolveApplicationName(config);
        }

        public ElasticSearchErrorLog(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public override string Log(Error error)
        {
            var indexResponse = _elasticClient.Index(new ErrorDocument
            {
                ApplicationName = ApplicationName,
                ErrorXml = ErrorXml.EncodeString(error),
                Detail = error.Detail,
                HostName = error.HostName,
                Message = error.Message,
                Source = error.Source,
                StatusCode = error.StatusCode,
                Time = error.Time,
                Type = error.Type,
                User = error.User,
                WebHostHtmlMessage = error.WebHostHtmlMessage,
            });

            if (!indexResponse.IsValid)
            {
                throw new ApplicationException(string.Format("Could not log error to elasticsearch: {0}",
                    indexResponse.ConnectionStatus));
            }

            return indexResponse.Id;
        }

        public override ErrorLogEntry GetError(string id)
        {
            var errorDoc = _elasticClient.Get<ErrorDocument>(x => x.Id(id));
            var error = ErrorXml.DecodeString(errorDoc.Source.ErrorXml);
            error.ApplicationName = ApplicationName;
            var result = new ErrorLogEntry(this, id, error);
            return result;
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            var result = _elasticClient.Search<ErrorDocument>(x => x
                .Filter(f => f.Term(t => t.ApplicationName, ApplicationName))
                .Skip(pageSize*pageIndex)
                .Take(pageSize)
                .Sort(s => s.OnField(e => e.Time).Descending())
                );

            foreach (var errorDocHit in result.Hits)
            {
                var error = ErrorXml.DecodeString(errorDocHit.Source.ErrorXml);
                error.ApplicationName = ApplicationName;
                errorEntryList.Add(new ErrorLogEntry(this, errorDocHit.Id, error));
            }

            return (int) result.Total;
        }

        private string LoadConnectionString(IDictionary config)
        {
            // From ELMAH source
            // First look for a connection string name that can be 
            // subsequently indexed into the <connectionStrings> section of 
            // the configuration to get the actual connection string.

            var connectionStringName = (string) config["connectionStringName"];

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (settings != null)
                    return settings.ConnectionString;

                throw new ApplicationException(string.Format("Could not find a ConnectionString with the name '{0}'.", connectionStringName));
            }

            throw new ApplicationException("You must specifiy the 'connectionStringName' attribute on the <errorLog /> element.");
        }

        private void InitElasticSearch(IDictionary config)
        {
            var url = LoadConnectionString(config);

            var defaultIndex = GetDefaultIndex(config, url);
            var conString = RemoveDefaultIndexFromConnectionString(url);
            var conSettings = new ConnectionSettings(new Uri(conString), defaultIndex);
            _elasticClient = new ElasticClient(conSettings);


            if (!_elasticClient.IndexExists(new IndexExistsRequest(defaultIndex)).Exists)
            {
                var createIndexResult = _elasticClient.CreateIndex(defaultIndex, c => c
                    .NumberOfReplicas(0)
                    .NumberOfShards(1)
                    .Settings(s => s
                        .Add("merge.policy.merge_factor", "10")
                        .Add("search.slowlog.threshold.fetch.warn", "1s"))
                    .AddMapping<ErrorDocument>(m => m.MapFromAttributes())
                    );

                if (!createIndexResult.IsValid)
                {
                    throw new ApplicationException(string.Format("Could not create elasticsearch ELMAH index:{0}",
                        createIndexResult.ConnectionStatus));
                }
            }
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
            if (defaultConnectionString != null)
            {
                return defaultConnectionString;
            }

            //step 2: we couldn't find it in the connection string so get it from the elmah config section or use the default
            return !string.IsNullOrWhiteSpace(config["defaultIndex"] as string) ? config["defaultIndex"].ToString().ToLower() : "elmah";
        }

        internal static string GetDefaultIndexFromConnectionString(string connectionString)
        {
            //Remove the trailing "/" if any
            connectionString = RemoveTrailingSlash(connectionString);

            //Get the URL authority
            string leftPart = RemoveDefaultIndexFromConnectionString(connectionString);
            string rightPart = string.Empty;

            //If leftpart is smaller than connectionstring
            //that means we have a index in there
            if (connectionString.Length > leftPart.Length)
            {
                rightPart = connectionString.Substring(leftPart.Length + 1);
            }
            return (!string.IsNullOrWhiteSpace(rightPart)) ? rightPart : null;
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

        internal static string ResolveApplicationName(IDictionary config)
        {
            return config.Contains("applicationName") ? config["applicationName"].ToString() : string.Empty;
        }
    }
}
