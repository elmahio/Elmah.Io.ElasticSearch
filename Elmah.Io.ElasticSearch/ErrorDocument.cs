using System;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    [ElasticType(Name = "errorLog")]
    public class ErrorDocument
    {
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.No)]
        public string ErrorXml { get; set; }

        public string ApplicationName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed)]
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
    }
}