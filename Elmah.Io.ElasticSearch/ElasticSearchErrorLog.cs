using System;
using System.Collections;
using System.Configuration;
using System.Runtime.CompilerServices;
using Nest;

[assembly: InternalsVisibleTo("Elmah.Io.ElasticSearch.Tests")]
namespace Elmah.Io.ElasticSearch
{
    public class ElasticSearchErrorLog : ErrorLog
    {
        private const string MultiFieldSuffix = "raw";
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
            indexResponse.VerifySuccessfulResponse();

            return indexResponse.Id;
        }

        public override ErrorLogEntry GetError(string id)
        {
            var errorDoc = _elasticClient.Get<ErrorDocument>(x => x.Id(id));
            errorDoc.VerifySuccessfulResponse();
            var error = ErrorXml.DecodeString(errorDoc.Source.ErrorXml);
            error.ApplicationName = ApplicationName;
            var result = new ErrorLogEntry(this, id, error);
            return result;
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            var result = _elasticClient.Search<ErrorDocument>(x => x
                .Filter(f => f
                    .Term("applicationName.raw", ApplicationName))
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .Sort(s => s.OnField(e => e.Time).Descending())
                );
            result.VerifySuccessfulResponse();

            foreach (var errorDocHit in result.Hits)
            {
                var error = ErrorXml.DecodeString(errorDocHit.Source.ErrorXml);
                error.ApplicationName = ApplicationName;
                errorEntryList.Add(new ErrorLogEntry(this, errorDocHit.Id, error));
            }

            return (int)result.Total;
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

        private void InitElasticSearch(IDictionary config)
        {
            var url = LoadConnectionString(config);

            var defaultIndex = GetDefaultIndex(config, url);
            var conString = RemoveDefaultIndexFromConnectionString(url);
            var conSettings = new ConnectionSettings(new Uri(conString), defaultIndex);
            _elasticClient = new ElasticClient(conSettings);


            if (!_elasticClient.IndexExists(new IndexExistsRequest(defaultIndex)).Exists)
            {
                _elasticClient.CreateIndex(defaultIndex).VerifySuccessfulResponse();
                _elasticClient.Map<ErrorDocument>(m => m
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
                    )
                )
                .VerifySuccessfulResponse();

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

        internal static string ResolveApplicationName(IDictionary config)
        {
            return config.Contains("applicationName") ? config["applicationName"].ToString() : string.Empty;
        }
    }
}
