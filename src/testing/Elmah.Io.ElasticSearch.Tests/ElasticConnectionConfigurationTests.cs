using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Elmah.Io.ElasticSearch.Tests
{
    [TestFixture]
    internal class ElasticConnectionConfigurationTests
    {
        private ElasticConnectionConfiguration _service;

        [TestFixtureSetUp]
        public void Setup()
        {
            _service = new ElasticConnectionConfiguration();
        }

        private static IEnumerable<TestCaseData> NodesTestCases
        {
            get
            {
                yield return new TestCaseData("Nodes=https://test:9200;DefaultIndex=defaultIndex")
                    .Returns(new[] { new Uri("https://test:9200") });
                yield return new TestCaseData("DefaultIndex=defaultIndex;Nodes=https://test:9200")
                    .Returns(new[] { new Uri("https://test:9200") });
                yield return new TestCaseData("Nodes=https://test:9200,http://ben:9300/;DefaultIndex=defaultIndex")
                    .Returns(new[] { new Uri("https://test:9200"), new Uri("http://ben:9300/") });
                yield return new TestCaseData(" Nodes=https://test:9200")
                    .Returns(new[] { new Uri("https://test:9200") });
                yield return new TestCaseData(null)
                    .Throws<ArgumentNullException>();
                yield return new TestCaseData(string.Empty)
                    .Throws<ArgumentException>();
                yield return new TestCaseData("DefaultIndex=defaultIndex")
                    .Throws<ArgumentException>();
            }
        }

        [TestCaseSource("NodesTestCases")]
        public IEnumerable<Uri> ParseNodes(string connectionString)
        {
            return _service.ParseCsv(connectionString, ElasticConnectionConfiguration.NodesKey);
        }

        private static IEnumerable<TestCaseData> DefaultIndexTestCases
        {
            get
            {
                yield return new TestCaseData("Nodes=https://test:9200;DefaultIndex=defaultIndex;")
                    .Returns("defaultIndex");
                yield return new TestCaseData("DefaultIndex=defaultIndex;DefaultIndex=defaultIndex2;")
                    .Returns("defaultIndex");
                yield return new TestCaseData("Nodes=https://test:9200; DefaultIndex=defaultIndex;")
                    .Returns("defaultIndex");
                yield return new TestCaseData("DefaultIndex=defaultIndex;Nodes=https://test:9200;")
                    .Returns("defaultIndex");
                yield return new TestCaseData("Nodes=https://test:9200")
                    .Returns(null);
                yield return new TestCaseData(null)
                    .Throws<ArgumentNullException>();
                yield return new TestCaseData(string.Empty)
                    .Returns(null);
            }
        }

        [TestCaseSource("DefaultIndexTestCases")]
        public string ParseDefaultIndex(string connectionString)
        {
            return _service.ParseSingle(connectionString, ElasticConnectionConfiguration.DefaultIndexKey);
        }

        private static IEnumerable<TestCaseData> UsernameTestCases
        {
            get
            {
                yield return new TestCaseData("Nodes=https://test:9200;DefaultIndex=defaultIndex;Username=test;")
                    .Returns("test");
                yield return new TestCaseData("DefaultIndex=defaultIndex;DefaultIndex=defaultIndex2;Username=test")
                    .Returns("test");
                yield return new TestCaseData("Nodes=https://test:9200; Username=test;DefaultIndex=defaultIndex2")
                    .Returns("test");
                yield return new TestCaseData("Username=test;DefaultIndex=defaultIndex;Nodes=https://test:9200;")
                    .Returns("test");
                yield return new TestCaseData("Nodes=https://test:9200")
                    .Returns(null);
                yield return new TestCaseData(null)
                    .Throws<ArgumentNullException>();
                yield return new TestCaseData(string.Empty)
                    .Returns(null);
            }
        }

        [TestCaseSource("UsernameTestCases")]
        public string ParseUsername(string connectionString)
        {
            return _service.ParseSingle(connectionString, ElasticConnectionConfiguration.UsernameKey);
        }

        private static IEnumerable<TestCaseData> PasswordTestCases
        {
            get
            {
                yield return new TestCaseData("Nodes=https://test:9200;Password=pass")
                    .Returns("pass");
                yield return new TestCaseData("DefaultIndex=defaultIndex;Password=pass;")
                    .Returns("pass");
                yield return new TestCaseData("Nodes=https://test:9200; Password=pass;")
                    .Returns("pass");
                yield return new TestCaseData("Password=pass;Nodes=https://test:9200;")
                    .Returns("pass");
                yield return new TestCaseData("Nodes=https://test:9200")
                    .Returns(null);
                yield return new TestCaseData(null)
                    .Throws<ArgumentNullException>();
                yield return new TestCaseData(string.Empty)
                    .Returns(null);
            }
        }

        [TestCaseSource("PasswordTestCases")]
        public string ParsePassword(string connectionString)
        {
            return _service.ParseSingle(connectionString, ElasticConnectionConfiguration.PasswordKey);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static IEnumerable<TestCaseData> GetDefaultIndexTests()
        {
            //straight index, no date format
            yield return new TestCaseData("DefaultIndex=abc123").Returns("abc123");

            //valid date formats
            const string fmt1 = "yyyy.MM.dd";
            const string fmt2 = "yyyy.MM";
            yield return new TestCaseData(string.Format("DefaultIndex=${{{0}}}", fmt1)).Returns(DateTimeOffset.Now.ToString(fmt1));
            yield return new TestCaseData(string.Format("DefaultIndex=prefix_${{{0}}}", fmt1)).Returns("prefix_" + DateTimeOffset.Now.ToString(fmt1));
            yield return new TestCaseData(string.Format("DefaultIndex=prefix_${{{0}}}_suffix", fmt1)).Returns(string.Format("prefix_{0}_suffix", DateTimeOffset.Now.ToString(fmt1)));
            yield return new TestCaseData(string.Format("DefaultIndex=${{{0}}}", fmt2)).Returns(DateTimeOffset.Now.ToString(fmt2));

            const string invalidFormat = "123456789";//garbage format
            yield return new TestCaseData(string.Format("DefaultIndex=${{{0}}}", invalidFormat)).Returns(invalidFormat);

            //no default index specified
            yield return new TestCaseData("Nodes=https://test:9200;Password=pass").Returns(null);
        }

        /// <summary>
        /// test the default index method which includes date parsing
        /// </summary>
        [TestCaseSource("GetDefaultIndexTests")]
        public string GetDefaultIndex(string connectionString)
        {
            return _service.GetDefaultIndex(connectionString);
        }

        [Test]
        public void Parse_CallsGetDefaultIndex()
        {
            //arrange
            var serviceLocal = new Mock<ElasticConnectionConfiguration>
            {
                CallBase = true
            };
            const string connectionString = "Nodes=https://test:9200,http://test2:9300/;DefaultIndex=defaultIndex";

            //act
            serviceLocal.Object.Parse(connectionString);

            //assert
            serviceLocal.Verify(x => x.GetDefaultIndex(connectionString), Times.Once);
        }
    }
}
