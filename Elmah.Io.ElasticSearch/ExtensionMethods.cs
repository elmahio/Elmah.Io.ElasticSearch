using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using JetBrains.Annotations;
using Nest;

namespace Elmah.Io.ElasticSearch
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Debugging method to get the json that was sent to ES
        /// </summary>
        public static string GetRequestString<T>(this ISearchResponse<T> response) where T : class
        {
            var request = response.RequestInformation.Request;
            return Encoding.Default.GetString(request);
        }

        /// <summary>
        /// ensure that the ES command was run successfully on the server
        /// </summary>
        public static void VerifySuccessfulResponse(this IResponse response)
        {
            if (!response.IsValid)
            {
                throw new ElasticsearchServerException(response.ServerError);
            }
        }

        /// <summary>
        /// Used to replace if (object == null) throw new ArgumentNullException("object");
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="paramName"></param>
        [ContractAnnotation("obj:null => halt")]
        public static void ThrowIfNull<T>([NoEnumeration] this T obj, string paramName) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// trim the string, then if it is null or empty, return null
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string TrimToNull(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || !list.Any();
        }
    }
}
