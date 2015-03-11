using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Moq;
using NUnit.Framework;
using Nest;
using Ploeh.AutoFixture;

namespace Elmah.Io.ElasticSearch.Tests
{
    public class ElasticSearchErrorLogTest
    {
        [Test]
        public void CanGetErrors()
        {
            // Arrange
            var fixture = new Fixture();
            var id1 = fixture.Create<string>();
            var id2 = fixture.Create<string>();
            var applicationName = fixture.Create<string>();

            var error1 = new Error(new Exception("error1"));
            var error2 = new Error(new Exception("error2"));
            var errorXml1 = ErrorXml.EncodeString(error1);
            var errorXml2 = ErrorXml.EncodeString(error2);
            var errorDoc1 = new ErrorDocument {ErrorXml = errorXml1};
            var errorDoc2 = new ErrorDocument {ErrorXml = errorXml2};

            var elasticClientMock = new Mock<IElasticClient>();
            var queryResponse = new Mock<ISearchResponse<ErrorDocument>>();

            queryResponse.Setup(x => x.Total).Returns(2);
            queryResponse.Setup(x => x.Hits).Returns(() =>
            {
                var mockHit1 = new Mock<IHit<ErrorDocument>>();
                mockHit1.Setup(x => x.Id).Returns(id1);
                mockHit1.Setup(x => x.Source).Returns(errorDoc1);

                var mockHit2 = new Mock<IHit<ErrorDocument>>();
                mockHit2.Setup(x => x.Id).Returns(id2);
                mockHit2.Setup(x => x.Source).Returns(errorDoc2);

                return new[]
                {
                    mockHit1.Object,
                    mockHit2.Object
                };
            });
            queryResponse.Setup(x => x.IsValid).Returns(true);

            elasticClientMock
                .Setup(x => x.Search(It.IsAny<Func<SearchDescriptor<ErrorDocument>, SearchDescriptor<ErrorDocument>>>()))
                .Returns(queryResponse.Object);

            var errorLog = new ElasticSearchErrorLog(elasticClientMock.Object)
            {
                ApplicationName = applicationName,
            };

            // Act
            var result = new ArrayList();
            var count = errorLog.GetErrors(0, int.MaxValue, result);

            // Assert
            Assert.That(count, Is.EqualTo(2));
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void CanGetError()
        {
            // Arrange
            var fixture = new Fixture();
            const string id = "mock error id";
            var applicationName = fixture.Create<string>();

            var error = new Error(new HttpException());
            var errorXml = ErrorXml.EncodeString(error);

            var mockResponse = new Mock<IGetResponse<ErrorDocument>>();
            mockResponse.Setup(x => x.Source).Returns(new ErrorDocument { ErrorXml = errorXml });
            mockResponse.Setup(x => x.IsValid).Returns(true);

            var elasticClientMock = new Mock<IElasticClient>();
            elasticClientMock
                .Setup(x => x.Get(It.IsAny<Func<GetDescriptor<ErrorDocument>, GetDescriptor<ErrorDocument>>>()))
                .Returns(mockResponse.Object);

            var errorLog = new ElasticSearchErrorLog(elasticClientMock.Object)
            {
                ApplicationName = applicationName,
            };

            // Act
            var elmahError = errorLog.GetError(id);

            // Assert
            Assert.That(elmahError != null);
            Assert.That(elmahError.Id, Is.EqualTo(id));
            Assert.That(elmahError.Error != null);
            Assert.That(elmahError.Error.ApplicationName, Is.EqualTo(applicationName));
        }

        [Test]
        public void CanLogError()
        {
            // Arrange
            var fixture = new Fixture();
            var id = fixture.Create<string>();
            var applicationName = fixture.Create<string>();
            var error = new Error(new HttpException());

            var elasticClientMock = new Mock<IElasticClient>();
            var responseMock = new Mock<IIndexResponse>();

            responseMock.Setup(x => x.Id).Returns(id);
            responseMock.Setup(x => x.IsValid).Returns(true);
            elasticClientMock
                .Setup(x => x.Index(It.IsAny<ErrorDocument>(), It.IsAny<Func<IndexDescriptor<ErrorDocument>, IndexDescriptor<ErrorDocument>>>()))
                .Returns(responseMock.Object);

            var errorLog = new ElasticSearchErrorLog(elasticClientMock.Object)
                {
                    ApplicationName = applicationName,
                };

            // Act
            var returnId = errorLog.Log(error);

            // Assert
            Assert.That(returnId, Is.EqualTo(id));
            elasticClientMock.Verify(
                x =>
                x.Index(
                    It.Is<ErrorDocument>(
                        d =>
                        d.ApplicationName == applicationName 
                        && d.Detail == error.Detail 
                        && d.HostName == error.HostName 
                        && d.Message == error.Message 
                        && d.Source == error.Source 
                        && d.StatusCode == error.StatusCode 
                        && d.Time == error.Time 
                        && d.Type == error.Type 
                        && d.User == error.User 
                        && d.WebHostHtmlMessage == error.WebHostHtmlMessage
                        ), It.IsAny<Func<IndexDescriptor<ErrorDocument>, IndexDescriptor<ErrorDocument>>>()));
        }

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
            var defaultIndex = ElasticSearchErrorLog.GetDefaultIndex(dict, connectionString);

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
            var defaultIndex = ElasticSearchErrorLog.GetDefaultIndexFromConnectionString(connectionString);

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
            var connectionStringOnly = ElasticSearchErrorLog.RemoveDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.AreEqual(expectedResult, connectionStringOnly);
        }

        [Test]
        public void ResolveApplicationName_Specified()
        {
            //arrange
            const string expectedAppName = "app123";
            var config = new Hashtable { { "applicationName", expectedAppName } };
            
            //act
            var appName = ElasticSearchErrorLog.ResolveApplicationName(config);

            //assert
            Assert.AreEqual(expectedAppName, appName);
        }

        [Test]
        public void ResolveApplicationName_NotSpecified()
        {
            //arrange
            var config = new Hashtable();
            
            //act
            var appName = ElasticSearchErrorLog.ResolveApplicationName(config);

            //assert
            Assert.AreEqual(string.Empty, appName);
        }

        [TestCase("", "")]
        [TestCase("http://localhost:9200", "http://localhost:9200")]
        [TestCase("http://localhost:9200/", "http://localhost:9200")]
        [TestCase("http://localhost:9200/indexHere/", "http://localhost:9200/indexHere")]
        [TestCase("http://localhost:9200/indexHere", "http://localhost:9200/indexHere")]
        public void RemoveTralingSlash(string origString, string expectedString)
        {
            string newString = ElasticSearchErrorLog.RemoveTrailingSlash(origString);

            Assert.AreEqual(expectedString, newString);
        }
    }
}