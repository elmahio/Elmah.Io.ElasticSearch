using System;
using System.Collections;
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
            var queryResponse = new Mock<IQueryResponse<ErrorDocument>>();

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

        [Test]
        public void CanGetError()
        {
            // Arrange
            var fixture = new Fixture();
            var id = fixture.Create<string>();
            var applicationName = fixture.Create<string>();

            var error = new Error(new HttpException());
            var errorXml = ErrorXml.EncodeString(error);

            var elasticClientMock = new Mock<IElasticClient>();
            elasticClientMock
                .Setup(x => x.Get<ErrorDocument>(id))
                .Returns(new ErrorDocument {ErrorXml = errorXml});

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
            elasticClientMock.Setup(x => x.Index(It.IsAny<ErrorDocument>())).Returns(responseMock.Object);

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
                        d.ApplicationName == applicationName && d.Detail == error.Detail && d.HostName == error.HostName &&
                        d.Id != null && d.Id.Length > 0 && d.Message == error.Message && d.Source == error.Source &&
                        d.StatusCode == error.StatusCode && d.Time == error.Time && d.Type == error.Type &&
                        d.User == error.User && d.WebHostHtmlMessage == error.WebHostHtmlMessage)));
        }
    }
}