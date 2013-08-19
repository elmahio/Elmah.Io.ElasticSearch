using System;
using System.Collections;
using System.Configuration;
using System.Security.Cryptography;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    public class ElasticSearchErrorLog : ErrorLog
    {
        IElasticClient _elasticClient;

        public ElasticSearchErrorLog(IDictionary config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            InitElasticSearch(config);
        }

        public ElasticSearchErrorLog(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public override string Log(Error error)
        {
            var indexResponse = _elasticClient.Index(new ErrorDocument
                {
                    Id = GenerateUniqueId(),
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
            var document = _elasticClient.Get<ErrorDocument>(id);
            var error = ErrorXml.DecodeString(document.ErrorXml);
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

            foreach (var errorDocument in result.Documents)
            {
                var error = ErrorXml.DecodeString(errorDocument.ErrorXml);
                error.ApplicationName = ApplicationName;
                errorEntryList.Add(new ErrorLogEntry(this, errorDocument.Id, error));
            }

            return result.Total;
        }

        private string LoadConnectionString(IDictionary config)
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

        private void InitElasticSearch(IDictionary config)
        {
            var defaultIndex = !string.IsNullOrWhiteSpace(config["defaultIndex"] as string)
                                   ? config["defaultIndex"].ToString().ToLower()
                                   : "elmah";
            var url = LoadConnectionString(config);
            var connectionSettings = new ConnectionSettings(new Uri(url));
            connectionSettings.SetDefaultIndex(defaultIndex);
            _elasticClient = new ElasticClient(connectionSettings);

            if (!_elasticClient.IndexExists(defaultIndex).Exists)
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
        /// We'll generate an _id for ElasticSearch so it's a predictable format
        /// </summary>
        /// <returns></returns>
        private string GenerateUniqueId()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                // change the size of the array depending on your requirements
                var rndBytes = new byte[8];
                rng.GetBytes(rndBytes);
                return BitConverter.ToString(rndBytes).Replace("-", "");
            }
        }
    }
}
