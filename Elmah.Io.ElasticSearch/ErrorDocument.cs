using System;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    /// <summary>
    /// All multi-fields have the suffix of ".raw" for the not_analyzed version
    /// </summary>
    [ElasticType(Name = "error")]
    public class ErrorDocument
    {
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.No)]
        public string ErrorXml { get; set; }

        public string ApplicationName { get; set; }

        public string HostName { get; set; }

        public string Type { get; set; }

        public string Source { get; set; }

        public string Message { get; set; }

        public string Detail { get; set; }

        public string User { get; set; }

        [ElasticProperty(Name = "@timestamp")]
        public DateTime Time { get; set; }

        public int StatusCode { get; set; }

        public string WebHostHtmlMessage { get; set; }

        public string EnvironmentName { get; set; }

        public string CustomerName { get; set; }
    }
}