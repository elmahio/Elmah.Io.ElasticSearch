﻿using System;
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

            var error1 = new Error(new HttpException());
            var error2 = new Error(new HttpException());
            var errorXml1 = ErrorXml.EncodeString(error1);
            var errorXml2 = ErrorXml.EncodeString(error2);
            var errorDoc1 = new ErrorDocument { ErrorXml = errorXml1, Id = id1 };
            var errorDoc2 = new ErrorDocument { ErrorXml = errorXml2, Id = id2 };

            var elasticClientMock = new Mock<IElasticClient>();
            var queryResponse = new Mock<ISearchResponse<ErrorDocument>>();

            queryResponse.Setup(x => x.Total).Returns(2);
            queryResponse.Setup(x => x.Documents).Returns(new[] {errorDoc1, errorDoc2});

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

        /// <summary>
        /// with the update to Nest 1.0 the Get<T> method is now an extension
        /// method and mocking those are not straight forward.  Ignoring the test for now.
        /// </summary>
        [Test]
        [Ignore]
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

            var elasticClientMock = new Mock<IElasticClient>();
            elasticClientMock
                .Setup(x => x.Get<ErrorDocument>(id, It.IsAny<string>(), It.IsAny<string>()))
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
        /// test getting the default index from the connection string
        /// </summary>
        [Test]
        public void GetDefaultIndexFromConnectionString_NotNull()
        {
            //arrange
            const string expectedDefaultIndex = "indexTest";
            const string connectionString = "http://localhost:9200/" + expectedDefaultIndex;

            //act 
            var defaultIndex = ElasticSearchErrorLog.GetDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.AreEqual(expectedDefaultIndex, defaultIndex);
        }

        /// <summary>
        /// test getting the default index from the connection string, in this test it does not exist
        /// </summary>
        [Test]
        public void GetDefaultIndexFromConnectionString_Null()
        {
            //arrange
            const string connectionString = "http://localhost:9200/";

            //act 
            var defaultIndex = ElasticSearchErrorLog.GetDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.Null(defaultIndex);
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

        [TestCase("http://localhost:9200/", "http://localhost:9200")]
        [TestCase("http://localhost:9200", "http://localhost:9200")]
        [TestCase("http://localhost:9200/defaultIndex123", "http://localhost:9200")]
        public void RemoveDefaultIndexFromConnectionString_DefaultIndexNull(string connectionString, string expectedResult)
        {
            //act 
            var connectionStringOnly = ElasticSearchErrorLog.RemoveDefaultIndexFromConnectionString(connectionString);

            //assert
            Assert.AreEqual(expectedResult, connectionStringOnly);
        }
    }
}