using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Elmah.Io.ElasticSearch
{
    public class ElasticSearchClusterConfiguration
    {
        public IEnumerable<Uri> NodeUris { get; set; }
        public string DefaultIndex { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
    }


    internal interface IElasticConnectionConfiguration
    {
        /// <summary>
        /// Introduced in release 1.2, this is the preferred way of specifying the connection string.  It supports:
        /// 1. multiple ES nodes
        /// 2. Shield (username/password)
        /// 3. specifying a default index
        /// </summary>
        /// <example>
        /// "Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar"
        /// </example>
        ElasticSearchClusterConfiguration Parse(string connectionString);

    }

    internal class ElasticConnectionConfiguration : IElasticConnectionConfiguration
    {
        internal const string DefaultIndexKey = "DefaultIndex=";
        internal const string NodesKey = "Nodes=";
        internal const string UsernameKey = "Username=";
        internal const string PasswordKey = "Password=";

        /// <summary>
        /// Introduced in release 1.2, this is the preferred way of specifying the connection string.  It supports:
        /// 1. multiple ES nodes
        /// 2. Shield (username/password)
        /// 3. specifying a default index
        /// </summary>
        /// <example>
        /// "Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar"
        /// </example>
        public ElasticSearchClusterConfiguration Parse(string connectionString)
        {
            if (!connectionString.Contains(NodesKey))
            {
                //connection string is supplied in a deprecated format
                return null;
            }
            var config = new ElasticSearchClusterConfiguration
            {
                NodeUris = ParseCsv(connectionString, NodesKey),
                DefaultIndex = GetDefaultIndex(connectionString),
                Username = ParseSingle(connectionString, UsernameKey),
                Password = ParseSingle(connectionString, PasswordKey)
            };

            if (config.NodeUris.IsNullOrEmpty())
            {
                throw new ArgumentException(string.Format("At least one Node must be specified.  Provided connection string: \"{0}\".  Example of a valid connection string: Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar", connectionString));
            }

            if (config.DefaultIndex == null)
            {
                throw new ArgumentException(string.Format("A DefaultIndex must be specified.  Provided connection string: \"{0}\".  Example of a valid connection string: Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar", connectionString));
            }
            return config;
        }

        internal virtual string GetDefaultIndex(string connectionString)
        {
            var indexName = ParseSingle(connectionString, DefaultIndexKey);
            if (indexName == null)
            {
                return null;
            }
            var dateFormatRegex = new Regex("\\${(.+)}");
            var match = dateFormatRegex.Match(indexName);
            if (!match.Success)
            {
                return indexName;
            }
            var dateFormat = match.Groups[1].Value;
            var dt = DateTimeOffset.Now.ToString(dateFormat);

            var newIndexName = dateFormatRegex.Replace(indexName, dt);
            return newIndexName;
        }

        internal string ParseSingle(string connectionString, string key)
        {
            connectionString.ThrowIfNull("connectionString");

            var connectionStringSegments = connectionString
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var defaultIndexSegment = connectionStringSegments.FirstOrDefault(p => p.StartsWith(key, StringComparison.OrdinalIgnoreCase));

            return defaultIndexSegment?.Substring(key.Length).TrimToNull();
        }

        internal IEnumerable<Uri> ParseCsv(string connectionString, string key)
        {
            connectionString.ThrowIfNull("connectionString");

            var connectionStringSegments = connectionString
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var nodesSegmemt = connectionStringSegments.FirstOrDefault(p => p.Trim().StartsWith(key, StringComparison.OrdinalIgnoreCase));

            if (nodesSegmemt == null)
            {
                throw new ArgumentException("Connection string must contain a 'Nodes' segment");
            }

            return nodesSegmemt
                .Substring(key.Length) //strip of the "nodes=" at the front
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ns => new Uri(ns));
        }
    }
}
