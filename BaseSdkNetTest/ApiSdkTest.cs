using BaseSDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace BaseSdkNetTest
{
    [TestClass]
    public class ApiSdkTest
    {
        private const string APP_ID = "iy4G8PgdwxZ6z4KhaGDK";
        private const string APP_SECRET = "sEuLkTNfPfBpZJ3bwHs4FvixsQbdDqppi8kB4rcz";

        private TestSimulationSetup apiSdk;

        [TestInitialize]
        public void Configure()
        {
            apiSdk = new TestSimulationSetup(APP_ID, APP_SECRET);
        }

        [TestMethod]
        public void TestSignature()
        {
            apiSdk.TestGetUrl<object>();
            Assert.AreEqual(HttpVerbs.Get.ToString(), apiSdk.Method);
            Assert.AreEqual(TestSimulationSetup.API_HOST + TestSimulationSetup.DEFAULT_URL, apiSdk.URL.AbsoluteUri);
            Assert.AreEqual("2015-06-23 12:48:17", apiSdk.Headers[ApiAuth.DATE_HEADER_NAME]);
            Assert.AreEqual("11PATHS iy4G8PgdwxZ6z4KhaGDK UWB6n+14BNvQsz403ku0D8yt9B4=", apiSdk.Headers[ApiAuth.AUTHORIZATION_HEADER_NAME]);
            Assert.IsNull(apiSdk.Data);
            Assert.IsNull(apiSdk.FileContent);
            Assert.IsFalse(apiSdk.Headers.ContainsKey(ApiAuth.BODY_HASH_HEADER_NAME));
        }

        [TestMethod]
        public void TestSignatureWithParams()
        {
            IDictionary<string, string> data = new Dictionary<string, string>();
            data.Add("name", "Api");
            data.Add("lastName", "Sdk test");
            apiSdk.TestParamsPostUrl<object>(data);
            Assert.AreEqual(HttpVerbs.Post.ToString(), apiSdk.Method);
            Assert.AreEqual(TestSimulationSetup.API_HOST + TestSimulationSetup.DEFAULT_URL, apiSdk.URL.AbsoluteUri);
            Assert.AreEqual(data, apiSdk.Data);
            Assert.AreEqual("2015-06-23 12:48:17", apiSdk.Headers[ApiAuth.DATE_HEADER_NAME]);
            Assert.AreEqual("11PATHS iy4G8PgdwxZ6z4KhaGDK iL3BGCQHbUQxBVhhDqN4KM/JrrI=", apiSdk.Headers[ApiAuth.AUTHORIZATION_HEADER_NAME]);
            Assert.AreEqual(ApiAuth.HTTP_HEADER_CONTENT_TYPE_FORM_URLENCODED, apiSdk.ContentType);
            Assert.IsNull(apiSdk.FileContent);
            Assert.IsFalse(apiSdk.Headers.ContainsKey(ApiAuth.BODY_HASH_HEADER_NAME));
        }

        [TestMethod]
        public void TestSignatureWithBody()
        {
            apiSdk.TestJsonPostUrl<object>();
            Assert.AreEqual(HttpVerbs.Post.ToString(), apiSdk.Method);
            Assert.AreEqual(TestSimulationSetup.API_HOST + TestSimulationSetup.DEFAULT_URL, apiSdk.URL.AbsoluteUri);
            Assert.AreEqual("11PATHS iy4G8PgdwxZ6z4KhaGDK 702wxyw1LLA5SnnYy1u4Zz1DT74=", apiSdk.Headers[ApiAuth.AUTHORIZATION_HEADER_NAME]);
            Assert.AreEqual("406c917764ee655103d12a961d28a221bd8c8d98", apiSdk.Headers[ApiAuth.BODY_HASH_HEADER_NAME]);
            Assert.AreEqual("2015-06-23 12:48:17", apiSdk.Headers[ApiAuth.DATE_HEADER_NAME]);
            Assert.AreEqual(ApiAuth.HTTP_HEADER_CONTENT_TYPE_JSON, apiSdk.ContentType);
            Assert.IsNull(apiSdk.Data);
            Assert.IsNull(apiSdk.FileContent);
        }

        [TestMethod]
        public void TestSignatureWithMultipart()
        {
            string text = "Api SDK test.";
            byte[] fileContent = null;
            using (MemoryStream s = TestSimulationSetup.GenerateStreamFromString(text))
            {
                fileContent = s.ToArray();
                apiSdk.TestFilePostUrl<object>(fileContent, "TestFile.txt");
            }
            Assert.AreEqual(HttpVerbs.Post.ToString(), apiSdk.Method);
            Assert.AreEqual(TestSimulationSetup.API_HOST + TestSimulationSetup.DEFAULT_URL, apiSdk.URL.AbsoluteUri);
            Assert.IsNotNull(apiSdk.Headers[ApiAuth.FILE_HASH_HEADER_NAME]);
            Assert.AreEqual("11PATHS iy4G8PgdwxZ6z4KhaGDK deJd5LZsXAWPhXPItTks08bc2gI=", apiSdk.Headers[ApiAuth.AUTHORIZATION_HEADER_NAME]);
            Assert.AreEqual("2015-06-23 12:48:17", apiSdk.Headers[ApiAuth.DATE_HEADER_NAME]);
            Assert.AreEqual("9e969e1514ba88b89e2b147220166ce7e4732509", apiSdk.Headers[ApiAuth.FILE_HASH_HEADER_NAME]);
            Assert.AreEqual(fileContent, apiSdk.FileContent);
            Assert.AreEqual(ApiAuth.MULTIPART_FORM_DATA, apiSdk.ContentType);
            Assert.IsNull(apiSdk.Data);
            Assert.IsFalse(apiSdk.Headers.ContainsKey(ApiAuth.BODY_HASH_HEADER_NAME));
        }
    }
}