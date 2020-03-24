using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace BaseSDK
{
    public enum HttpVerbs
    {
        Get,
        Post,
        Put,
        Delete
    }

    public abstract class ApiAuth
    {
        public const string AUTHORIZATION_HEADER_NAME = "Authorization";
        public const string BODY_HASH_HEADER_NAME = X_11PATHS_HEADER_PREFIX + "Body-Hash";
        public const string DATE_HEADER_NAME = "X-11Paths-Date";
        public const string DATE_NAME = "Date";
        public const string FILE_HASH_HEADER_NAME = X_11PATHS_HEADER_PREFIX + "File-Hash";
        public const string HTTP_HEADER_CONTENT_TYPE_FORM_URLENCODED = "application/x-www-form-urlencoded";
        public const string HTTP_HEADER_CONTENT_TYPE_JSON = "application/json";
        public const string MULTIPART_FORM_DATA = "multipart/form-data";

        public Uri BaseUrl { get; }

        protected const char AUTHORIZATION_HEADER_FIELD_SEPARATOR = ' ';
        protected const char PARAM_SEPARATOR = '&';
        protected const char PARAM_VALUE_SEPARATOR = '=';
        protected const char X_11PATHS_HEADER_SEPARATOR = ':';
        protected const string AUTHORIZATION_METHOD = "11PATHS";
        protected const string QUERYSTRING_DELIMITER = "?";
        protected const string UTC_STRING_FORMAT = "yyyy-MM-dd HH:mm:ss";
        protected const string X_11PATHS_HEADER_PREFIX = "X-11paths-";
        protected static readonly byte[] LINE_BREAK_BYTES = { 0x0d, 0x0a };

        protected string AppId { get; }
        protected string SecretKey { get; }

        private WebProxy proxy;

        /// <summary>
        /// Creates an instance of the class with the <code>Application ID</code> and <code>Secret</code> obtained from Eleven Paths
        /// </summary>
        protected ApiAuth(string baseUrl, string appId, string secretKey)
        {
            if (String.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (String.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException(nameof(appId));

            if (String.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentNullException(nameof(secretKey));

            Uri url;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out url))
            {
                throw new ArgumentException("Invalid uri format", nameof(baseUrl));
            }
            else
            {
                this.BaseUrl = url;
            }

            this.AppId = appId;
            this.SecretKey = secretKey;
        }

        protected ApiAuth(string baseUrl, string appId, string secretKey, WebProxy proxy) : this(baseUrl, appId, secretKey)
        {
            if (proxy == null)
                throw new ArgumentNullException(nameof(proxy));

            this.proxy = proxy;
        }

        protected ApiResponse<T> GetHttpRequest<T>(string url, IDictionary<string, string> queryParams)
        {
            try
            {
                string urlWithParams = String.Concat(url, ParseQueryParams(queryParams));
                return GetHttpRequest<T>(new Uri(this.BaseUrl, urlWithParams), AuthenticationHeaders(HttpVerbs.Get, urlWithParams));
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        private ApiResponse<T> GetHttpRequest<T>(Uri url, IDictionary<string, string> headers)
        {
            return HttpRequest<T>(url, HttpVerbs.Get, headers, null);
        }

        protected ApiResponse<T> DeleteHttpRequest<T>(string url)
        {
            try
            {
                return DeleteHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeaders(HttpVerbs.Delete, url));
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        private ApiResponse<T> DeleteHttpRequest<T>(Uri url, IDictionary<string, string> headers)
        {
            return HttpRequest<T>(url, HttpVerbs.Delete, headers, null);
        }

        protected ApiResponse<T> PostHttpRequest<T>(string url, IDictionary<string, string> data)
        {
            try
            {
                return PostHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeadersWithParams(HttpVerbs.Post, url, data), data);
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        protected ApiResponse<T> PostHttpRequest<T>(string url, string body)
        {
            try
            {
                return PostHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeadersWithBody(HttpVerbs.Post, url, body), body);
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        protected ApiResponse<T> PostHttpRequest<T>(string url, byte[] fileContent, string filename)
        {
            try
            {
                return PostHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeadersWithFile(HttpVerbs.Post, url, fileContent), fileContent, filename);
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        private ApiResponse<T> PostHttpRequest<T>(Uri url, IDictionary<string, string> headers, IDictionary<string, string> data)
        {
            return HttpRequest<T>(url, HttpVerbs.Post, headers, data);
        }

        private ApiResponse<T> PostHttpRequest<T>(Uri url, IDictionary<string, string> headers, string body)
        {
            return HttpRequest<T>(url, HttpVerbs.Post, headers, body, null, null, HTTP_HEADER_CONTENT_TYPE_JSON);
        }

        private ApiResponse<T> PostHttpRequest<T>(Uri url, IDictionary<string, string> headers, byte[] fileContent, string filename)
        {
            return HttpRequest<T>(url, HttpVerbs.Post, headers, null, fileContent, filename, MULTIPART_FORM_DATA);
        }

        protected ApiResponse<T> PutHttpRequest<T>(string url, IDictionary<string, string> data)
        {
            return PutHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeadersWithParams(HttpVerbs.Put, url, data), data);
        }

        protected ApiResponse<T> PutHttpRequest<T>(string url, string body)
        {
            return PutHttpRequest<T>(new Uri(this.BaseUrl, url), AuthenticationHeadersWithBody(HttpVerbs.Put, url, body), body);
        }

        private ApiResponse<T> PutHttpRequest<T>(Uri url, IDictionary<string, string> headers, IDictionary<string, string> data)
        {
            return HttpRequest<T>(url, HttpVerbs.Put, headers, data);
        }

        private ApiResponse<T> PutHttpRequest<T>(Uri url, IDictionary<string, string> headers, string body)
        {
            return HttpRequest<T>(url, HttpVerbs.Put, headers, body, null, null, HTTP_HEADER_CONTENT_TYPE_JSON);
        }

        private IDictionary<string, string> AuthenticationHeaders(HttpVerbs httpMethod, string absoluteUriPath)
        {
            return AuthenticationHeadersWithParams(httpMethod, absoluteUriPath, null);
        }

        /// <summary>
        /// Calculate the authentication headers to be sent with a request to the API
        /// </summary>
        /// <param name="httpMethod">The HTTP Method, currently only GET is supported</param>
        /// <param name="absoluteUriPath">The urlencoded string including the path (from the first forward slash) and the parameters.</param>
        /// <param name="params">The HTTP request params. Must be only those to be sent in the body of the request and must be urldecoded. Null if not needed.</param>
        /// <returns>A map with the Authorization and X-11Paths-Date headers needed to sign a Latch API request</returns>
        private IDictionary<string, string> AuthenticationHeadersWithParams(HttpVerbs httpMethod, string absoluteUriPath, IDictionary<string, string> param)
        {
            if (String.IsNullOrWhiteSpace(absoluteUriPath))
            {
                return null;
            }
            return CalculateSignedHeaders(httpMethod, absoluteUriPath, null, GetCurrentUTC(), param);
        }

        private IDictionary<string, string> AuthenticationHeadersWithBody(HttpVerbs httpMethod, string absoluteUriPath, string body)
        {
            byte[] bodyBytes = (body != null) ? Encoding.UTF8.GetBytes(body) : null;
            return AuthenticationHeadersWithBody(httpMethod, absoluteUriPath, bodyBytes, GetCurrentUTC());
        }

        private IDictionary<string, string> AuthenticationHeadersWithFile(HttpVerbs httpMethod, string absoluteUriPath, byte[] fileContent)
        {
            return AuthenticationHeadersWithFile(httpMethod, absoluteUriPath, fileContent, GetCurrentUTC());
        }

        /// <summary>
        /// Calculates the headers to be sent with a request to the API so the server can verify the signature
        /// </summary>
        /// <param name="httpMethod">The HTTP request method.</param>
        /// <param name="absoluteUriPath">The urlencoded string including the path (from the first forward slash) and the parameters.</param>
        /// <param name="body">The HTTP request body. Null if not needed.</param>
        /// <param name="utcDate">The Universal Coordinated Time for the X-11Paths-Date HTTP header</param>
        /// <returns>A map with the {@value #AUTHORIZATION_HEADER_NAME}, the {@value #DATE_HEADER_NAME} and the {@value #BODY_HASH_HEADER_NAME} headers needed to be sent with a request to the API.</returns>
        private IDictionary<string, string> AuthenticationHeadersWithBody(HttpVerbs httpMethod, string absoluteUriPath, byte[] body, string utcDate)
        {
            if (String.IsNullOrWhiteSpace(absoluteUriPath) || String.IsNullOrWhiteSpace(utcDate))
            {
                return null;
            }

            Dictionary<string, string> xHeaders = null;
            if (body != null)
            {
                xHeaders = new Dictionary<string, string>() { { BODY_HASH_HEADER_NAME, Utils.Sha1(body) } };
            }

            IDictionary<string, string> headers = CalculateSignedHeaders(httpMethod, absoluteUriPath, xHeaders, utcDate, null);
            if (body != null)
            {
                headers.Add(BODY_HASH_HEADER_NAME, xHeaders[BODY_HASH_HEADER_NAME]);
            }
            return headers;
        }

        private IDictionary<string, string> AuthenticationHeadersWithFile(HttpVerbs httpMethod, string absoluteUriPath, byte[] fileContent, string utcDate)
        {
            if (String.IsNullOrWhiteSpace(absoluteUriPath) || String.IsNullOrWhiteSpace(utcDate))
            {
                return null;
            }

            Dictionary<string, string> xHeaders = null;
            if (fileContent != null)
            {
                xHeaders = new Dictionary<string, string>() { { FILE_HASH_HEADER_NAME, Utils.Sha1(fileContent) } };
            }

            IDictionary<string, string> headers = CalculateSignedHeaders(httpMethod, absoluteUriPath, xHeaders, utcDate, null);

            if (fileContent != null)
            {
                headers.Add(FILE_HASH_HEADER_NAME, xHeaders[FILE_HASH_HEADER_NAME]);
            }
            return headers;
        }

        private IDictionary<string, string> CalculateSignedHeaders(HttpVerbs httpMethod, string absoluteUriPath, IDictionary<string, string> xHeaders, string utcDate, IDictionary<string, string> param)
        {
            string stringToSign = String.Concat(httpMethod.ToString().ToUpper(), "\n", utcDate, "\n", SerializeHeaders(xHeaders), "\n", absoluteUriPath.Trim());
            {
                string serializedParams = SerializeParams(param);
                if (!String.IsNullOrWhiteSpace(serializedParams))
                {
                    stringToSign = String.Concat(stringToSign, "\n", serializedParams);
                }
            }

            string signedData = String.Empty;
            try
            {
                signedData = SignData(stringToSign);
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }

            string authorizationHeader = String.Concat(AUTHORIZATION_METHOD, AUTHORIZATION_HEADER_FIELD_SEPARATOR, this.AppId, AUTHORIZATION_HEADER_FIELD_SEPARATOR, signedData);

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(AUTHORIZATION_HEADER_NAME, authorizationHeader);
            headers.Add(DATE_HEADER_NAME, utcDate);
            return headers;
        }

        /// <summary>
        /// Signs the data provided in order to prevent tampering
        /// </summary>
        /// <param name="data">The string to sign</param>
        /// <returns>Base64 encoding of the HMAC-SHA1 hash of the data parameter using <code>secretKey</code> as cipher key.</returns>
        private string SignData(string data)
        {
            if (String.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("String to sign can not be null or empty.");
            }

            using (HMACSHA1 hmacSha1 = new HMACSHA1(Encoding.ASCII.GetBytes(SecretKey)))
            {
                return Convert.ToBase64String(hmacSha1.ComputeHash(Encoding.ASCII.GetBytes(data)));
            }
        }

        /// <summary>
        /// Performs an HTTP request to an URL using the specified method and data, returning the response as a string
        /// </summary>
        protected virtual ApiResponse<T> HttpRequest<T>(Uri url, HttpVerbs method, IDictionary<string, string> headers, IDictionary<string, string> data)
        {
            string body = SerializeParams(data);
            return HttpRequest<T>(url, method, headers, body, null, null, HTTP_HEADER_CONTENT_TYPE_FORM_URLENCODED);
        }

        /// <summary>
        /// Performs an HTTP request to an URL using the specified method, headers and data, returning the response as a string
        /// </summary>
        protected virtual ApiResponse<T> HttpRequest<T>(Uri url, HttpVerbs method, IDictionary<string, string> headers, string body, byte[] fileContent, string filename, string contentType)
        {
            HttpWebRequest request = BuildHttpUrlConnection(url, headers, method);

            try
            {
                if (method.Equals(HttpVerbs.Post) || method.Equals(HttpVerbs.Put))
                {
                    if (body != null && fileContent == null)
                    {
                        request.ContentType = contentType;
                        using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                        {
                            streamWriter.Write(body);
                            streamWriter.Flush();
                        }
                    }
                    else
                    {
                        string boundary = GetMillisecondsFromEpoch(DateTime.UtcNow).ToString("x4");
                        request.ContentType = String.Format(@"{0}; boundary=""{1}""", MULTIPART_FORM_DATA, boundary);

                        using (BinaryWriter streamWriter = new BinaryWriter(request.GetRequestStream()))
                        {
                            streamWriter.Write(Encoding.UTF8.GetBytes("--" + boundary));
                            streamWriter.Write(LINE_BREAK_BYTES);
                            streamWriter.Write(Encoding.UTF8.GetBytes("Content-Disposition: form-data; name=file; filename=" + filename));
                            streamWriter.Write(LINE_BREAK_BYTES);
                            streamWriter.Write(LINE_BREAK_BYTES);

                            streamWriter.Write(fileContent);
                            streamWriter.Write(LINE_BREAK_BYTES);

                            streamWriter.Write(Encoding.UTF8.GetBytes("--" + boundary + "--"));
                            streamWriter.Flush();
                        }
                    }
                }

                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    string json = sr.ReadToEnd();
                    return new JavaScriptSerializer().Deserialize<ApiResponse<T>>(json);
                }
            }
            catch (Exception e)
            {
                Tracer.Instance.TraceAndOutputError(e.ToString());
                return null;
            }
        }

        private HttpWebRequest BuildHttpUrlConnection(Uri url, IDictionary<string, string> headers, HttpVerbs method)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method.ToString().ToUpper();
            if (this.proxy != null)
            {
                request.Proxy = this.proxy;
            }

            foreach (string key in headers.Keys)
            {
                if (key.Equals(AUTHORIZATION_HEADER_NAME, StringComparison.InvariantCultureIgnoreCase))
                {
                    request.Headers[AUTHORIZATION_HEADER_NAME] = headers[key];
                }
                else if (key.Equals(DATE_NAME, StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        request.Date = DateTime.Parse(headers[key], null, System.Globalization.DateTimeStyles.AssumeUniversal);
                    }
                    catch (Exception e)
                    {
                        Tracer.Instance.TraceAndOutputError(e.ToString());
                        return null;
                    }
                }
                else
                {
                    request.Headers.Add(key, headers[key]);
                }
            }
            return request;
        }

        /// <summary>
        /// Returns a string representation of the current time in UTC to be used in a Date HTTP Header
        /// </summary>
        protected virtual string GetCurrentUTC()
        {
            return DateTime.UtcNow.ToString(UTC_STRING_FORMAT);
        }

        public static long GetMillisecondsFromEpoch(DateTime date)
        {
            return (long)(date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        /// <summary>
        /// Encodes a string to be passed as an URL parameter in UTF-8
        /// </summary>
        public static string UrlEncode(string value)
        {
            return WebUtility.UrlEncode(value);
        }

        public static string UrlPathEncode(string value)
        {
            return Uri.EscapeDataString(value);
        }

        #region Static Methods

        private static string ParseQueryParams(IDictionary<string, string> queryParams)
        {
            if (queryParams == null || queryParams.Count == 0)
            {
                return String.Empty;
            }

            return QUERYSTRING_DELIMITER + String.Join(PARAM_SEPARATOR.ToString(), queryParams.Select(p => String.Concat(UrlPathEncode(p.Key), PARAM_VALUE_SEPARATOR, UrlPathEncode(p.Value))));
        }

        /// <summary>
        /// Prepares and returns a string ready to be signed from the 11-paths specific HTTP headers received
        /// </summary>
        /// <param name="xHeaders">A non necessarily sorted IDictionary of the HTTP headers</param>
        /// <returns>A string with the serialized headers, an empty string if no headers are passed, or a ApplicationException if there's a problem
        ///  such as non specific 11paths headers</returns>
        private static string SerializeHeaders(IDictionary<string, string> xHeaders)
        {
            if (xHeaders != null)
            {
                StringBuilder serializedHeaders = new StringBuilder();
                foreach (var currentHeader in xHeaders.Where(p => p.Key.StartsWith(X_11PATHS_HEADER_PREFIX, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Key))
                {
                    serializedHeaders.Append(String.Concat(currentHeader.Key.ToLowerInvariant(), X_11PATHS_HEADER_SEPARATOR, currentHeader.Value.Replace('\n', ' '), AUTHORIZATION_HEADER_FIELD_SEPARATOR));
                }

                return serializedHeaders.ToString().Trim(AUTHORIZATION_HEADER_FIELD_SEPARATOR);
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Prepares and returns a string ready to be signed from the parameters of an HTTP request
        /// </summary>
        /// <param name="parameters">A non necessarily sorted IDictionary of the parameters</param>
        /// <returns>A string with the serialized parameters, an empty string if no headers are passed</returns>
        /// <remarks> The params must be only those included in the body of the HTTP request when its content type
        ///     is application/x-www-urlencoded and must be urldecoded. </remarks>
        private static string SerializeParams(IDictionary<string, string> parameters)
        {
            if (parameters != null)
            {
                StringBuilder serializedParams = new StringBuilder();
                foreach (var currentParam in parameters.OrderBy(p => p.Key))
                {
                    serializedParams.Append(String.Concat(UrlEncode(currentParam.Key), PARAM_VALUE_SEPARATOR, UrlEncode(currentParam.Value), PARAM_SEPARATOR));
                }

                return serializedParams.ToString().Trim(PARAM_SEPARATOR);
            }
            else
            {
                return String.Empty;
            }
        }

        #endregion Static Methods
    }
}