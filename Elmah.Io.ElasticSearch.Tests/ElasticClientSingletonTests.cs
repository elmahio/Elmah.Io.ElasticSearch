using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Elmah.Io.ElasticSearch.Tests
{
    [TestFixture]
    public class ElasticClientSingletonTests
    {
        /// <summary>
        /// test getting the default from the elmah config instead of from the connection string
        /// </summary>
        [Test]
        public void GetDefaultIndex_FromElmahConfig()
        {
            //arrange
            const string connectionString = "http://localhost:9200/";
            const string expectedDefaultIndex = "defaultFromConfig";
            var dict = new Dictionary<string, string>
            {
                {"defaultIndex", expectedDefaultIndex}
            };

            //act 
            var defaultIndex = ElasticClientSingleton.GetDefaultIndex(dict, connectionString);

            //assert
            Assert.AreEqual(expectedDefaultIndex.ToLower(), defaultIndex);
        }

        [TestCase("http://localhost:9200", "")]
        [TestCase("http://localhost:9200/", "")]
        [TestCase("http://localhost:9200/indexHere", "indexHere")]
        [TestCase("http://localhost:9200/indexHere/", "indexHere")]
        [TestCase("http://localhost:9201/indexHere", "indexHere")]
        [TestCase("http://localhost/indexHere", "indexHere")]
        public void GetDefaultIndexFromConnectionString(string connectionString, string expectedResult)
        {
            //act 
            var defaultIndex = ElasticClientSingleton.GetDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.AreEqual(expectedResult, defaultIndex);
        }

        [TestCase("http://localhost:9200/", "http://localhost:9200")]
        [TestCase("http://localhost:9200", "http://localhost:9200")]
        [TestCase("http://localhost:9200/defaultIndex123", "http://localhost:9200")]
        [TestCase("http://localhost:9201/defaultIndex123", "http://localhost:9201")]
        [TestCase("http://localhost/defaultIndex123", "http://localhost")]
        public void RemoveDefaultIndexFromConnectionString(string connectionString, string expectedResult)
        {
            //act 
            var connectionStringOnly = ElasticClientSingleton.RemoveDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.AreEqual(expectedResult, connectionStringOnly);
        }

        [TestCase("", "")]
        [TestCase("http://localhost:9200", "http://localhost:9200")]
        [TestCase("http://localhost:9200/", "http://localhost:9200")]
        [TestCase("http://localhost:9200/indexHere/", "http://localhost:9200/indexHere")]
        [TestCase("http://localhost:9200/indexHere", "http://localhost:9200/indexHere")]
        public void RemoveTralingSlash(string origString, string expectedString)
        {
            string newString = ElasticClientSingleton.RemoveTrailingSlash(origString);

            Assert.AreEqual(expectedString, newString);
        }
    }
}
