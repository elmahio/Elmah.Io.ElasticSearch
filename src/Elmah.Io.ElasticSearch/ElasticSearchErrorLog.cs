using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Nest;

[assembly: InternalsVisibleTo("Elmah.Io.ElasticSearch.Tests")]
namespace Elmah.Io.ElasticSearch
{
    public class ElasticSearchErrorLog : ErrorLog
    {
        internal string CustomerName;
        internal string EnvironmentName;

        private static IElasticClient _elasticClient;

        // ReSharper disable once UnusedMember.Global
        public ElasticSearchErrorLog(IDictionary config)
        {
            InitializeConfigParameters(config);

            _elasticClient = ElasticClientSingleton.GetInstance(config).Client;        
        }

        /// <summary>
        /// This constructor is only used in unit tests...which is not ideal
        /// </summary>
        public ElasticSearchErrorLog(IElasticClient elasticClient, IDictionary config)
        {
            InitializeConfigParameters(config);

            _elasticClient = elasticClient;
        }

        private void InitializeConfigParameters(IDictionary config)
        {
            config.ThrowIfNull("config");
            ApplicationName = ResolveConfigurationParam(config, "applicationName");
            EnvironmentName = ResolveConfigurationParam(config, "environmentName");
            CustomerName = ResolveConfigurationParam(config, "customerName");
        }

        public override string Log(Error error)
        {                   
            var indexResponse = _elasticClient.Index
                (new IndexRequest<ErrorDocument>(new ErrorDocument
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
                    CustomerName = CustomerName,
                    EnvironmentName = EnvironmentName,
                    ServerVariables = ConvertToKeyValue(error.ServerVariables)
                }));

            indexResponse.VerifySuccessfulResponse();

            return indexResponse.Id;
        }

        private Dictionary<string, string> ConvertToKeyValue(NameValueCollection serverVariables)
        {
            var dict = new Dictionary<string, string>();

            foreach (var serverVariable in serverVariables.AllKeys)
            {
                dict.Add(serverVariable, serverVariables[serverVariable]);
            }      

            return dict;
        }

        public override ErrorLogEntry GetError(string id)
        {
            var errorDoc = _elasticClient.Get<ErrorDocument>(id).VerifySuccessfulResponse();
            var error = ErrorXml.DecodeString(errorDoc.Source.ErrorXml);
            error.ApplicationName = ApplicationName;
            var result = new ErrorLogEntry(this, id, error);
            return result;
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {

            var result = _elasticClient.Search<ErrorDocument>(x => x
                    .Query(q => q
                        .Term("applicationName", ApplicationName)
                    )
                    .Skip(pageSize * pageIndex)
                    .Take(pageSize)
                    .Sort(s => s
                        .Descending(d => d.Time)
                    )
                )
                .VerifySuccessfulResponse();


            //var result = _elasticClient.Search<ErrorDocument>(x => x
            //    .Query(q=> q
            //       .Term("applicationName.raw", ApplicationName)
            //     )
            //    .Skip(pageSize * pageIndex)
            //    .Take(pageSize)
            //    .Sort(s => s
            //        .Descending(d=> d.Time)
            //     )
            //    )
            //    .VerifySuccessfulResponse();

            //var debug = result.GetRequestString();

            foreach (var errorDocHit in result.Hits)
            {
                var error = ErrorXml.DecodeString(errorDocHit.Source.ErrorXml);
                error.ApplicationName = ApplicationName;
                errorEntryList.Add(new ErrorLogEntry(this, errorDocHit.Id, error));
            }

            return (int)result.Total;
        }

        internal static string ResolveConfigurationParam(IDictionary config, string key)
        {
            return config.Contains(key) ? config[key].ToString() : string.Empty;
        }
    }
}
