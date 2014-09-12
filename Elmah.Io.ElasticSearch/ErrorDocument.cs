using System;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    [ElasticType]
    public class ErrorDocument
    {
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.No)]
        public string ErrorXml { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed)]
        public string ApplicationName { get; set; }

        public string HostName { get; set; }

        public string Type { get; set; }

        public string Source { get; set; }

        public string Message { get; set; }

        public string Detail { get; set; }

        public string User { get; set; }

        public DateTime Time { get; set; }

        public int StatusCode { get; set; }

        public string WebHostHtmlMessage { get; set; }
    }
}