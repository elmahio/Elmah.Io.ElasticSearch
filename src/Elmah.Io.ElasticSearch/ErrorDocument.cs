using System;
using System.Collections.Generic;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    /// <summary>
    /// All multi-fields have the suffix of ".raw" for the not_analyzed version
    /// </summary>
    [ElasticsearchType(Name = "error")]
    public class ErrorDocument
    {
        public string Id { get; set; }

        [Text(Index = false)]
        public string ErrorXml { get; set; }

        [Text(Index = true, Fielddata = true)]
        public string ApplicationName { get; set; }

        [Text(Index = true, Fielddata = true)]
        public string HostName { get; set; }

        [Text(Index = true)]
        public string Type { get; set; }

        public string Source { get; set; }

        public string Message { get; set; }

        public string Detail { get; set; }

        public string User { get; set; }

        [Date(Name = "@timestamp")]
        public DateTime Time { get; set; }

        public int StatusCode { get; set; }

        public string WebHostHtmlMessage { get; set; }

        public string EnvironmentName { get; set; }

        [Text(Index = true, Fielddata = true)]
        public string CustomerName { get; set; }

        [Text(Index = true, Fielddata = true)]
        public string TenantId { get; set; }

        public Dictionary<string, string> ServerVariables { get; set; } = new Dictionary<string, string>();
    }


}