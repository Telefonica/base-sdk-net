using BaseSDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace BaseSdkNetTest
{
    internal class TestSimulationSetup : ApiAuth
    {
        public const string API_VERSION = "0.1";
        public const string API_HOST = "http://localhost:9000";
        public const string DEFAULT_URL = "/api/" + API_VERSION;

        private JsonBodyExample jsonBody = new JsonBodyExample();

        internal Uri URL { get; private set; }
        internal string Method { get; private set; }
        internal IDictionary<string, string> Headers { get; private set; }
        internal IDictionary<string, string> Data { get; private set; }
        internal string ContentType { get; private set; }
        internal string Body { get; private set; }
        internal byte[] FileContent { get; private set; }

        public TestSimulationSetup(string appId, string secretKey)
            : base(API_HOST, appId, secretKey)
        {
        }

        protected override ApiResponse<T> HttpRequest<T>(Uri URL, HttpVerbs method, IDictionary<string, string> headers, IDictionary<string, string> data)
        {
            this.URL = URL;
            this.Method = method.ToString();
            this.Headers = headers;
            this.Data = data;
            return base.HttpRequest<T>(URL, method, headers, data);
        }

        protected override ApiResponse<T> HttpRequest<T>(Uri URL, HttpVerbs method, IDictionary<string, string> headers, string body, byte[] fileContent, string filename, string contentType)
        {
            this.URL = URL;
            this.Method = method.ToString();
            this.Headers = headers;
            this.Body = body;
            this.FileContent = fileContent;
            this.ContentType = contentType;
            return null;
        }

        protected override string GetCurrentUTC()
        {
            return "2015-06-23 12:48:17";
        }

        public ApiResponse<T> TestGetUrl<T>()
        {
            return GetHttpRequest<T>(DEFAULT_URL, null);
        }

        public ApiResponse<T> TestParamsPostUrl<T>(IDictionary<string, string> data)
        {
            return PostHttpRequest<T>(DEFAULT_URL, data);
        }

        public ApiResponse<T> TestJsonPostUrl<T>()
        {
            return PostHttpRequest<T>(DEFAULT_URL, jsonBody.GetJsonEncoded());
        }

        public ApiResponse<T> TestFilePostUrl<T>(byte[] fileContent, string filename)
        {
            return PostHttpRequest<T>(DEFAULT_URL, fileContent, filename);
        }

        public static MemoryStream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}