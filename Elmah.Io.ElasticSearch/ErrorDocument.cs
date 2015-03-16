using System;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    /// <summary>
    /// All multi-fields have the suffix of ".raw" for the not_analyzed version
    /// </summary>
    [ElasticType]
    public class ErrorDocument
    {
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.No)]
        public string ErrorXml { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.  The not_analyzed version is applicationName.raw
        /// </summary>
        public string ApplicationName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed)]
        public string HostName { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.
        /// </summary>
        public string Message { get; set; }

        public string Detail { get; set; }

        public string User { get; set; }

        public DateTime Time { get; set; }

        public int StatusCode { get; set; }

        public string WebHostHtmlMessage { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// NOTE: this is a multi-field in ES.
        /// </summary>
        public string CustomerName { get; set; }
    }
}