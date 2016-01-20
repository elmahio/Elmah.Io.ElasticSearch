using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

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

        ElasticSearchClusterConfiguration BuildClusterConfigDeprecated(IDictionary config, string connectionString);
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
                DefaultIndex = ParseSingle(connectionString, DefaultIndexKey),
                Username = ParseSingle(connectionString, UsernameKey),
                Password = ParseSingle(connectionString, PasswordKey)
            };

            if (config.NodeUris.IsNullOrEmpty())
            {
                throw new ArgumentException($"At least one Node must be specified.  Provided connection string: \"{connectionString}\".  Example of a valid connection string: Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar");
            }

            if (config.DefaultIndex == null)
            {
                throw new ArgumentException($"A DefaultIndex must be specified.  Provided connection string: \"{connectionString}\".  Example of a valid connection string: Nodes=https://test:9200,http://dev:9300;DefaultIndex=defaultIndex;Username=foo;Password=bar");
            }
            return config;
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

        #region deprecated methods

        [Obsolete("this is here to support the ES connection string pre 1.2 release")]
        public ElasticSearchClusterConfiguration BuildClusterConfigDeprecated(IDictionary config, string connectionString)
        {
            var defaultIndex = GetDefaultIndex(config, connectionString);
            var conString = RemoveDefaultIndexFromConnectionString(connectionString);
            return new ElasticSearchClusterConfiguration
            {
                DefaultIndex = defaultIndex,
                NodeUris = new List<Uri> {new Uri(conString)}
            };
        }


        /// <summary>
        /// In the previous version the default index would come from the elmah configuration.
        /// 
        /// This version supports pulling the default index from the connection string which is cleaner and easier to manage.
        /// </summary>
        [Obsolete("this is here to support the ES connection string pre 1.2 release")]
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

        [Obsolete("this is here to support the ES connection string pre 1.2 release")]
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
        #endregion
    }
}
