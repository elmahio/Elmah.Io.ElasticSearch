﻿using System;
using System.Collections;
using System.Web;
using Moq;
using NUnit.Framework;
using Nest;
using Ploeh.AutoFixture;

namespace Elmah.Io.ElasticSearch.Tests
{
    [TestFixture]
    public class ElasticSearchErrorLogTest
    {
        private readonly Mock<IElasticClient> _elasticClientMock = new Mock<IElasticClient>();

        [Test]
        public void GetErrors()
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

            _elasticClientMock
                .Setup(x => x.Search(It.IsAny<Func<SearchDescriptor<ErrorDocument>, ISearchRequest>>()))
                .Returns(queryResponse.Object);

            var errorLog = new ElasticSearchErrorLog(_elasticClientMock.Object, new Hashtable())
            {
                ApplicationName = applicationName
            };

            // Act
            var result = new ArrayList();
            var count = errorLog.GetErrors(0, int.MaxValue, result);

            // Assert
            Assert.That(count, Is.EqualTo(2));
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetError()
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
                //.Setup(x => x.Get(id, It.IsAny<Func<GetDescriptor<ErrorDocument>, IGetRequest>>()))
                .Setup(x => x.Get(It.IsAny<DocumentPath<ErrorDocument>>(), It.IsAny<Func<GetDescriptor<ErrorDocument>, IGetRequest>>()))
                .Returns(mockResponse.Object);

            var errorLog = new ElasticSearchErrorLog(elasticClientMock.Object, new Hashtable())
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
        public void LogError()
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

            var errorLog = new ElasticSearchErrorLog(elasticClientMock.Object, new Hashtable())
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


        [Test]
        public void ResolveConfigurationParam_Specified()
        {
            //arrange
            const string key = "applicationName";
            const string expectedAppName = "app123";
            var config = new Hashtable { { key, expectedAppName } };
            
            //act
            var appName = ElasticSearchErrorLog.ResolveConfigurationParam(config, key);

            //assert
            Assert.AreEqual(expectedAppName, appName);
        }

        [Test]
        public void ResolveConfigurationParam_NotSpecified()
        {
            //arrange
            const string key = "applicationName";
            var config = new Hashtable();
            
            //act
            var appName = ElasticSearchErrorLog.ResolveConfigurationParam(config, key);

            //assert
            Assert.AreEqual(string.Empty, appName);
        }

        [Test]
        public void Constructor_SetApplicationName()
        {
            //arrange
            const string key = "applicationName";
            const string value = "app123";
            var config = new Hashtable
            {
                { key, value }
            };

            //act
            var log = new ElasticSearchErrorLog(_elasticClientMock.Object, config);

            //assert
            Assert.AreEqual(value, log.ApplicationName);
        }

        [Test]
        public void Constructor_SetEnvironmentName()
        {
            //arrange
            const string key = "environmentName";
            const string value = "app123";
            var config = new Hashtable
            {
                { key, value }
            };

            //act
            var log = new ElasticSearchErrorLog(_elasticClientMock.Object, config);

            //assert
            Assert.AreEqual(value, log.EnvironmentName);
        }

        [Test]
        public void Constructor_SetCustomerName()
        {
            //arrange
            const string key = "customerName";
            const string value = "app123";
            var config = new Hashtable
            {
                { key, value }
            };

            //act
            var log = new ElasticSearchErrorLog(_elasticClientMock.Object, config);

            //assert
            Assert.AreEqual(value, log.CustomerName);
        }
    }
}