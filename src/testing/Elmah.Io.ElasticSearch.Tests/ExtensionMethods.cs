using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Elmah.Io.ElasticSearch.Tests
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Wrapper for underlying <see cref="TestCaseData.Throws(System.Type)"/> using a generic for the type.
        /// </summary>
        public static TestCaseData Throws<TException>(this TestCaseData testCase)
        {
            return testCase.Throws(typeof(TException));
        }
    }
}
