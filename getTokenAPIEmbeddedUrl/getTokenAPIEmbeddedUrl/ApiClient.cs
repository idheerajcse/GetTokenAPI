﻿
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Security;
    using RestSharp;
  
    
    using Microsoft.IdentityModel.Tokens;
    using System.Runtime.Serialization;
    using DocuSign.eSign.Client.Auth;
    

    namespace DocuSign.eSign.Client
    {
        /// <summary>
        /// API client is mainly responsible for making the HTTP call to the API backend.
        /// </summary>
        public class ApiClient2
        {
            // Rest API base path constants
            // Live/Production base path
            public const string Production_REST_BasePath = "https://www.docusign.net/restapi";
            // Sandbox/Demo base path 
            public const string Demo_REST_BasePath = "https://demo.docusign.net/restapi";
            // Stage base path
            public const string Stage_REST_BasePath = "https://stage.docusign.net/restapi";

            private string basePath = Production_REST_BasePath;

            private string oAuthBasePath = OAuth.Production_OAuth_BasePath;

            private JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };

            /// <summary>
            /// Allows for extending request processing for <see cref="ApiClient"/> generated code.
            /// </summary>
            /// <param name="request">The RestSharp request object</param>
            public virtual void InterceptRequest(IRestRequest request)
            {
                // Override this to add telemetry
            }

            /// <summary>
            /// Allows for extending response processing for <see cref="ApiClient"/> generated code.
            /// </summary>
            /// <param name="request">The RestSharp request object</param>
            /// <param name="response">The RestSharp response object</param>
            public virtual void InterceptResponse(IRestRequest request, IRestResponse response)
            {
                // Override this to add telemetry
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiClient" /> class
            /// with default configuration and base path (https://www.docusign.net/restapi).
            /// </summary>
            public ApiClient2()
            {
                this.InitializeTLSProtocol();
                Configuration = Configuration.Default;
                RestClient = new RestClient("https://www.docusign.net/restapi");
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiClient" /> class
            /// with default base path (https://www.docusign.net/restapi).
            /// </summary>
            /// <param name="config">An instance of Configuration.</param>
            public ApiClient2(Configuration config)
            {
                this.InitializeTLSProtocol();

                Configuration = config ?? Configuration.Default;
                RestClient = new RestClient("https://www.docusign.net/restapi");
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiClient" /> class
            /// with default configuration.
            /// </summary>
            /// <param name="basePath">The base path.</param>
            /// <param name="proxy">An optional WebProxy instance.</param>
            public ApiClient2(String basePath = "https://www.docusign.net/restapi", WebProxy proxy = null)
            {
                if (String.IsNullOrEmpty(basePath))
                    throw new ArgumentException("basePath cannot be empty");

                this.InitializeTLSProtocol();

                this.Proxy = proxy;
                RestClient = new RestClient(basePath) { Proxy = proxy };
                Configuration = new Configuration(basePath);

                this.SetBasePath(basePath);
                this.SetOAuthBasePath();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ApiClient" /> class
            /// with default configuration.
            /// </summary>
            /// <param name="basePath">The base path.</param>
            /// <param name="oAuthBasePath">The oAuth base path.</param>
            /// <param name="proxy">An optional WebProxy instance.</param>
            public ApiClient2(String basePath, String oAuthBasePath, WebProxy proxy = null)
            {
                if (String.IsNullOrEmpty(basePath))
                    throw new ArgumentException("basePath cannot be empty");
                if (String.IsNullOrEmpty(oAuthBasePath))
                    throw new ArgumentException("oAuthBasePath cannot be empty");

                this.InitializeTLSProtocol();

                this.Proxy = proxy;
                RestClient = new RestClient(basePath) { Proxy = proxy };
                Configuration = new Configuration(basePath);

                this.SetBasePath(basePath);
                this.SetOAuthBasePath();
            }

            private void InitializeTLSProtocol()
            {
#if NETSTANDARD2_0
            // No-op, OS should decide which is the secure TLS protocol
#else
                SecurityProtocolType protocolVersions = new SecurityProtocolType();

                foreach (SecurityProtocolType securityProtocolType in
                    Enum.GetValues(typeof(SecurityProtocolType)))
                {
                    protocolVersions |= securityProtocolType;
                }

                protocolVersions &= ~SecurityProtocolType.Ssl3;
                protocolVersions &= ~SecurityProtocolType.Tls;
                ServicePointManager.SecurityProtocol = protocolVersions;
#endif
            }

            /// <summary>
            /// Gets or sets the Configuration.
            /// </summary>
            /// <value>An instance of the Configuration.</value>
            public Configuration Configuration { get; set; }

            /// <summary>
            /// Gets or sets the RestClient.
            /// </summary>
            /// <value>An instance of the RestClient</value>
            public RestClient RestClient { get; set; }

            // Creates and sets up a RestRequest prior to a call.
            private RestRequest PrepareRequest(
                String path, RestSharp.Method method, Dictionary<String, String> queryParams, Object postBody,
                Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
                Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
                String contentType)
            {
                var request = new RestRequest(path, method);

                // add path parameter, if any
                foreach (var param in pathParams)
                    request.AddParameter(param.Key, param.Value, ParameterType.UrlSegment);

                // DocuSign: Add DocuSign tracking headers
                request.AddHeader("X-DocuSign-SDK", "C#");

                // add header parameter, if any
                foreach (var param in headerParams)
                    request.AddHeader(param.Key, param.Value);

                // add query parameter, if any
                foreach (var param in queryParams)
                    request.AddQueryParameter(param.Key, param.Value);

                // add form parameter, if any
                foreach (var param in formParams)
                    request.AddParameter(param.Key, param.Value);

                // add file parameter, if any
                foreach (var param in fileParams)
                {
                    request.AddFile(param.Value.Name, param.Value.Writer, param.Value.FileName, param.Value.ContentLength, param.Value.ContentType);
                }

                if (postBody != null) // http body (model or byte[]) parameter
                {
                    if (String.IsNullOrEmpty(contentType))
                    {
                        contentType = "application/json";
                    }

                    if (postBody.GetType() == typeof(String) || postBody.GetType() == typeof(byte[]))
                    {
                        request.AddParameter(contentType, postBody, ParameterType.RequestBody);
                    }
                }

                return request;
            }

            /// <summary>
            /// Makes the HTTP request (Sync).
            /// </summary>
            /// <param name="path">URL path.</param>
            /// <param name="method">HTTP method.</param>
            /// <param name="queryParams">Query parameters.</param>
            /// <param name="postBody">HTTP body (POST request).</param>
            /// <param name="headerParams">Header parameters.</param>
            /// <param name="formParams">Form parameters.</param>
            /// <param name="fileParams">File parameters.</param>
            /// <param name="pathParams">Path parameters.</param>
            /// <param name="contentType">Content Type of the request</param>
            /// <returns>Object</returns>
            public Object CallApi(
                String path, RestSharp.Method method, Dictionary<String, String> queryParams, Object postBody,
                Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
                Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
                String contentType)
            {
                var request = PrepareRequest(
                    path, method, queryParams, postBody, headerParams, formParams, fileParams,
                    pathParams, contentType);

                // set timeout
                RestClient.Timeout = Configuration.Timeout;
                // set user agent
                RestClient.UserAgent = Configuration.UserAgent;

                InterceptRequest(request);
                var response = RestClient.Execute(request);
                InterceptResponse(request, response);

                return (Object)response;
            }
            /// <summary>
            /// Makes the asynchronous HTTP request.
            /// </summary>
            /// <param name="path">URL path.</param>
            /// <param name="method">HTTP method.</param>
            /// <param name="queryParams">Query parameters.</param>
            /// <param name="postBody">HTTP body (POST request).</param>
            /// <param name="headerParams">Header parameters.</param>
            /// <param name="formParams">Form parameters.</param>
            /// <param name="fileParams">File parameters.</param>
            /// <param name="pathParams">Path parameters.</param>
            /// <param name="contentType">Content type.</param>
            /// <returns>The Task instance.</returns>
            public async System.Threading.Tasks.Task<Object> CallApiAsync(
                String path, RestSharp.Method method, Dictionary<String, String> queryParams, Object postBody,
                Dictionary<String, String> headerParams, Dictionary<String, String> formParams,
                Dictionary<String, FileParameter> fileParams, Dictionary<String, String> pathParams,
                String contentType)
            {
                var request = PrepareRequest(
                    path, method, queryParams, postBody, headerParams, formParams, fileParams,
                    pathParams, contentType);
                InterceptRequest(request);
                var response = await RestClient.ExecuteAsync(request);
                InterceptResponse(request, response);
                return (Object)response;
            }

            /// <summary>
            /// Escape string (url-encoded).
            /// </summary>
            /// <param name="str">String to be escaped.</param>
            /// <returns>Escaped string.</returns>
            public string EscapeString(string str)
            {
                return UrlEncode(str);
            }

            /// <summary>
            /// Create FileParameter based on Stream.
            /// </summary>
            /// <param name="name">Parameter name.</param>
            /// <param name="stream">Input stream.</param>
            /// <returns>FileParameter.</returns>
            public FileParameter ParameterToFile(string name, Stream stream)
            {
                if (stream is FileStream)
                    return FileParameter.Create(name, ReadAsBytes(stream), Path.GetFileName(((FileStream)stream).Name));
                else
                    return FileParameter.Create(name, ReadAsBytes(stream), "no_file_name_provided");
            }

            /// <summary>
            /// If parameter is DateTime, output in a formatted string (default ISO 8601), customizable with Configuration.DateTime.
            /// If parameter is a list, join the list with ",".
            /// Otherwise just return the string.
            /// </summary>
            /// <param name="obj">The parameter (header, path, query, form).</param>
            /// <returns>Formatted string.</returns>
            public string ParameterToString(object obj)
            {
                if (obj is DateTime)
                    // Return a formatted date string - Can be customized with Configuration.DateTimeFormat
                    // Defaults to an ISO 8601, using the known as a Round-trip date/time pattern ("o")
                    // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#Anchor_8
                    // For example: 2009-06-15T13:45:30.0000000
                    return ((DateTime)obj).ToString(Configuration.DateTimeFormat);
                else if (obj is DateTimeOffset)
                    // Return a formatted date string - Can be customized with Configuration.DateTimeFormat
                    // Defaults to an ISO 8601, using the known as a Round-trip date/time pattern ("o")
                    // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#Anchor_8
                    // For example: 2009-06-15T13:45:30.0000000
                    return ((DateTimeOffset)obj).ToString(Configuration.DateTimeFormat);
                else if (obj is IList)
                {
                    var flattenedString = new StringBuilder();
                    foreach (var param in (IList)obj)
                    {
                        if (flattenedString.Length > 0)
                            flattenedString.Append(",");
                        flattenedString.Append(param);
                    }
                    return flattenedString.ToString();
                }
                else
                    return Convert.ToString(obj);
            }

            /// <summary>
            /// Deserialize the JSON string into a proper object.
            /// </summary>
            /// <param name="response">The HTTP response.</param>
            /// <param name="type">Object type.</param>
            /// <returns>Object representation of the JSON string.</returns>
            public object Deserialize(IRestResponse response, Type type)
            {
                IList<Parameter> headers = response.Headers;
                if (type == typeof(byte[])) // return byte array
                {
                    return response.RawBytes;
                }

                if (type == typeof(Stream))
                {
                    if (headers != null)
                    {
                        var filePath = String.IsNullOrEmpty(Configuration.TempFolderPath)
                            ? Path.GetTempPath()
                            : Configuration.TempFolderPath;
                        var regex = new Regex(@"Content-Disposition=.*filename=['""]?([^'""\s]+)['""]?$");
                        foreach (var header in headers)
                        {
                            var match = regex.Match(header.ToString());
                            if (match.Success)
                            {
                                string fileName = filePath + SanitizeFilename(match.Groups[1].Value.Replace("\"", "").Replace("'", ""));
                                File.WriteAllBytes(fileName, response.RawBytes);
                                return new FileStream(fileName, FileMode.Open);
                            }
                        }
                    }
                    var stream = new MemoryStream(response.RawBytes);
                    return stream;
                }

                if (type.Name.StartsWith("System.Nullable`1[[System.DateTime")) // return a datetime object
                {
                    return DateTime.Parse(response.Content, null, System.Globalization.DateTimeStyles.RoundtripKind);
                }

                if (type == typeof(String) || type.Name.StartsWith("System.Nullable")) // return primitive type
                {
                    return ConvertType(response.Content, type);
                }

                // at this point, it must be a model (json)
                try
                {
                    return JsonConvert.DeserializeObject(response.Content, type, serializerSettings);
                }
                catch (Exception e)
                {
                    throw new ApiException(500, e.Message);
                }
            }

            /// <summary>
            /// DocuSign: Deserialize the byte array into a proper object.
            /// </summary>
            /// <param name="content">Byte Araay (e.g. PDF bytes).</param>
            /// <param name="type">Object type.</param>
            /// <param name="headers"></param>
            /// <returns>Object representation of the JSON string.</returns>
            public object Deserialize(byte[] content, Type type, IList<Parameter> headers = null)
            {
                if (type == typeof(Stream))
                {
                    MemoryStream ms = new MemoryStream(content);
                    return ms;
                }

                throw new ApiException(500, "Unhandled response type.");
            }

            /// <summary>
            /// Serialize an input (model) into JSON string
            /// </summary>
            /// <param name="obj">Object.</param>
            /// <param name="contentType"></param>
            /// <returns>JSON string.</returns>
            public String Serialize(object obj, string contentType = "application/json")
            {
                try
                {
                    if (contentType == "text/csv")
                    {
                        return obj != null ? SerializeCsvToString(obj) : null;
                    }

                    return obj != null ? JsonConvert.SerializeObject(obj) : null;
                }
                catch (Exception e)
                {
                    throw new ApiException(500, e.Message);
                }
            }

            /// <summary>
            /// SerializeCsvToString - Interim method to Serialize the Request Object to CSV format
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static String SerializeCsvToString(object obj)
            {
                if (obj == null || obj.GetType() == typeof(String))
                    return null;

                StringBuilder sb = new StringBuilder();

                // We expect this object to be a List
                // Get the List Object which resids inside the RequestObject - needs improvement
                var requestObjList = obj.GetType().GetProperties()
                    .Select(n => n.GetValue(obj))
                    .ToList()
                    .FirstOrDefault();

                //for this iteration, we only support BulkRecipient request object 
                sb.Append(SerializeCsvToString((List<object>)requestObjList));

                return sb.ToString();
            }

            /// <summary>
            /// SerializeCsvToString - Interim method to Serialize the Request Object to CSV format
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static String SerializeCsvToString<T>(List<T> obj) where T : class
            {
                if (obj == null || obj.GetType() == typeof(String))
                    return null;

                string output = string.Empty;
                string csv = ",";

                var properties = typeof(T).GetProperties();

                using (var sw = new StringWriter())
                {
                    // Do this only once - get the DataMember name for each property
                    var headerRow = new StringBuilder();

                    foreach (var property in properties)
                    {
                        var dataMembers = property.GetCustomAttributes(typeof(DataMemberAttribute), true);
                        foreach (DataMemberAttribute attr in dataMembers)
                        {
                            headerRow.Append(attr.Name + csv);
                        }
                    }
                    headerRow.Remove(headerRow.Length - 1, 1);
                    sw.WriteLine(headerRow);

                    foreach (var item in obj)
                    {
                        // Get values of each in the given object
                        var row = properties
                            .Select(x => x.GetValue(item, null))
                            .Select(x => x == null ? "" : x.ToString())
                            .Aggregate((a, b) => a + csv + b);

                        sw.WriteLine(row);
                    }

                    output = sw.ToString();
                }
                return output;
            }

            /// <summary>
            /// Select the Content-Type header's value from the given content-type array:
            /// if JSON exists in the given array, use it;
            /// otherwise use the first one defined in 'consumes'
            /// </summary>
            /// <param name="contentTypes">The Content-Type array to select from.</param>
            /// <returns>The Content-Type header to use.</returns>
            public String SelectHeaderContentType(String[] contentTypes)
            {
                if (contentTypes.Length == 0)
                    return null;

                if (contentTypes.Contains("application/json", StringComparer.OrdinalIgnoreCase))
                    return "application/json";

                return contentTypes[0]; // use the first content type specified in 'consumes'
            }

            /// <summary>
            /// Select the Accept header's value from the given accepts array:
            /// if JSON exists in the given array, use it;
            /// otherwise use all of them (joining into a string)
            /// </summary>
            /// <param name="accepts">The accepts array to select from.</param>
            /// <returns>The Accept header to use.</returns>
            public String SelectHeaderAccept(String[] accepts)
            {
                if (accepts.Length == 0)
                    return null;

                if (accepts.Contains("application/json", StringComparer.OrdinalIgnoreCase))
                    return "application/json";

                return String.Join(",", accepts);
            }

            /// <summary>
            /// Encode string in base64 format.
            /// </summary>
            /// <param name="text">String to be encoded.</param>
            /// <returns>Encoded string.</returns>
            public static string Base64Encode(string text)
            {
                return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            }

            /// <summary>
            /// Dynamically cast the object into target type.
            /// Ref: http://stackoverflow.com/questions/4925718/c-dynamic-runtime-cast
            /// </summary>
            /// <param name="source">Object to be casted</param>
            /// <param name="dest">Target type</param>
            /// <returns>Casted object</returns>
            public static dynamic ConvertType(dynamic source, Type dest)
            {
                return Convert.ChangeType(source, dest);
            }

            /// <summary>
            /// Convert stream to byte array
            /// Credit/Ref: http://stackoverflow.com/a/221941/677735
            /// </summary>
            /// <param name="input">Input stream to be converted</param>
            /// <returns>Byte array</returns>
            public static byte[] ReadAsBytes(Stream input)
            {
                byte[] buffer = new byte[16 * 1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }

            /// <summary>
            /// URL encode a string
            /// Credit/Ref: https://github.com/restsharp/RestSharp/blob/master/RestSharp/Extensions/StringExtensions.cs#L50
            /// </summary>
            /// <param name="input">String to be URL encoded</param>
            /// <returns>Byte array</returns>
            public static string UrlEncode(string input)
            {
                const int maxLength = 32766;

                if (input == null)
                {
                    throw new ArgumentNullException("input");
                }

                if (input.Length <= maxLength)
                {
                    return Uri.EscapeDataString(input);
                }

                StringBuilder sb = new StringBuilder(input.Length * 2);
                int index = 0;

                while (index < input.Length)
                {
                    int length = Math.Min(input.Length - index, maxLength);
                    string subString = input.Substring(index, length);

                    sb.Append(Uri.EscapeDataString(subString));
                    index += subString.Length;
                }

                return sb.ToString();
            }

            /// <summary>
            /// Sanitize filename by removing the path
            /// </summary>
            /// <param name="filename">Filename</param>
            /// <returns>Filename</returns>
            public static string SanitizeFilename(string filename)
            {
                Match match = Regex.Match(filename, @".*[/\\](.*)$");

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    return filename;
                }
            }

            /// <summary>
            /// Gets or sets the Proxy of ApiClient. Default to null
            /// </summary>
            /// <value>Timeout.</value>
            public IWebProxy Proxy
            {
                get
                {
                    if (this.RestClient == null)
                        return null;

                    return this.RestClient.Proxy;
                }

                set
                {
                    if (this.RestClient != null)
                        this.RestClient.Proxy = value;
                }
            }

            /// <summary>
            /// Helper method to configure the OAuth accessCode/implicit flow parameters
            /// </summary>
            /// <param name="clientId">OAuth2 client ID: Identifies the client making the request.</param>
            /// <param name="scopes">the list of requested scopes.  Client applications may be scoped to a limited set of system access.</param>
            /// <param name="redirectUri">this determines where to deliver the response containing the authorization code or access token.</param>
            /// <param name="responseType">determines the response type of the authorization request.
            /// <br><i>Note</i>: these response types are mutually exclusive for a client application.
            /// A public/native client application may only request a response type of "token";
            /// a private/trusted client application may only request a response type of "code".</br></param>
            /// <param name="state">Allows for arbitrary state that may be useful to your application.
            /// The value in this parameter will be round-tripped along with the response so you can make sure it didn't change.</param>
            /// <returns></returns>
            public Uri GetAuthorizationUri(string clientId, List<string> scopes, string redirectUri, string responseType, string state = null)
            {
                string formattedScopes = (scopes == null || scopes.Count < 1) ? "" : scopes[0];
                StringBuilder scopesSb = new StringBuilder(formattedScopes);
                for (int i = 1; i < scopes.Count; i++)
                {
                    scopesSb.Append("%20" + scopes[i]);
                }

                UriBuilder builder = new UriBuilder(GetOAuthBasePath())
                {
                    Scheme = "https",
                    Path = "/oauth/auth",
                    Port = 443,
                    Query = BuildQueryString(clientId, scopesSb.ToString(), redirectUri, responseType, state)
                };
                return builder.Uri;
            }

            /// <summary>
            /// Builds a QueryString with the given parameters
            /// </summary>
            /// <param name="clientId"></param>
            /// <param name="scopes"></param>
            /// <param name="redirectUri"></param>
            /// <param name="responseType"></param>
            /// <param name="state"></param>
            /// <returns>Formatted Query String</returns>
            private string BuildQueryString(string clientId, string scopes, string redirectUri, string responseType, string state)
            {
                StringBuilder queryParams = new StringBuilder();
                if (!string.IsNullOrEmpty(responseType) || responseType != null)
                {
                    queryParams.Append("response_type=" + responseType);
                }
                if (!string.IsNullOrEmpty(scopes) || scopes != null)
                {
                    queryParams.Append("&scope=" + scopes);
                }
                if (!string.IsNullOrEmpty(clientId) || clientId != null)
                {
                    queryParams.Append("&client_id=" + clientId);
                }
                if (!string.IsNullOrEmpty(redirectUri) || redirectUri != null)
                {
                    queryParams.Append("&redirect_uri=" + redirectUri);
                }
                if (!string.IsNullOrEmpty(state) || state != null)
                {
                    queryParams.Append("&state=" + state);
                }

                return queryParams.ToString();
            }

            /// <summary>
            /// GetOAuthBasePath sets the basePath for the user account.
            /// </summary>
            /// <returns>If the current base path is demo then it sets the demo account as the basePath, else it sets the Production account as the basePath.</returns>
            private string GetOAuthBasePath()
            {
                if (string.IsNullOrEmpty(this.oAuthBasePath))
                {
                    this.SetOAuthBasePath();
                }
                return this.oAuthBasePath;
            }

            /// <summary>
            /// Use this method to Set Base Path
            /// </summary>
            /// <param name="basePath"></param>
            public void SetBasePath(string basePath)
            {
                this.basePath = basePath;
                if (Configuration != null)
                {
                    Configuration.BasePath = this.basePath;
                }
                else
                {
                    Configuration = new Configuration(this.basePath);
                }

                if (RestClient != null)
                {
                    RestClient.BaseUrl = new Uri(this.basePath);
                }
                else
                {
                    RestClient = new RestClient(this.basePath);
                }
            }

            /// <summary>
            /// Use this method to set custom OAuth Base Path.
            /// </summary>
            /// <param name="oAuthBasePath">Optional custom base path value. If not provided we will derive it according to the ApiClient basePath value.</param>
            public void SetOAuthBasePath(string oAuthBasePath = null)
            {
                //Set Custom Base path
                if (!string.IsNullOrEmpty(oAuthBasePath))
                {
                    this.oAuthBasePath = oAuthBasePath;
                    return;
                }

                //Derive OAuth Base Path if not given.
                if (this.basePath.StartsWith("https://demo") || this.basePath.StartsWith("http://demo"))
                {
                    this.oAuthBasePath = OAuth.Demo_OAuth_BasePath;
                }
                else if (this.basePath.StartsWith("https://stage") || this.basePath.StartsWith("http://stage"))
                {
                    this.oAuthBasePath = OAuth.Stage_OAuth_BasePath;
                }
                else
                {
                    this.oAuthBasePath = OAuth.Production_OAuth_BasePath;
                }
            }

            /// <summary>
            /// GenerateAccessToken will exchange the authorization code for an access token and refresh tokens.
            /// </summary>
            /// <param name="clientId">OAuth2 client ID: Identifies the client making the request.</param>
            /// <param name="clientSecret">the secret key you generated when you set up the integration in DocuSign Admin console.</param>
            /// <param name="code">The authorization code that you received from the <i> GetAuthorizationUri </i> callback.</param>
            /// <returns> OAuth.OAuthToken object.
            /// ApiException if the HTTP call status is different than 2xx.
            /// IOException  if there is a problem while parsing the reponse object.
            /// </returns>
            public OAuth.OAuthToken GenerateAccessToken(string clientId, string clientSecret, string code)
            {
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(code))
                {
                    throw new ArgumentNullException();
                }

                string baseUri = string.Format("https://{0}/", GetOAuthBasePath());

                string codeAuth = (clientId ?? "") + ":" + (clientSecret ?? "");
                byte[] codeAuthBytes = Encoding.UTF8.GetBytes(codeAuth);
                string codeAuthBase64 = Convert.ToBase64String(codeAuthBytes);

                RestClient restClient = new RestClient(baseUri);
                restClient.Timeout = Configuration.Timeout;
                restClient.UserAgent = Configuration.UserAgent;
                restClient.Proxy = Proxy;

                RestRequest request = new RestRequest("oauth/token", Method.POST);

                request.AddHeader("Authorization", "Basic " + codeAuthBase64);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                // Don't cache authentication requests
                request.AddHeader("Cache-Control", "no-store");
                request.AddHeader("Pragma", "no-cache");

                Dictionary<string, string> formParams = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code }
            };

                foreach (var item in formParams)
                    request.AddParameter(item.Key, item.Value);

                IRestResponse response = restClient.Execute(request);

                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
                {
                    OAuth.OAuthToken tokenObj = JsonConvert.DeserializeObject<OAuth.OAuthToken>(((RestResponse)response).Content);
                    // Add the token to this ApiClient
                    string authHeader = "Bearer " + tokenObj.access_token;
                    if (!this.Configuration.DefaultHeader.ContainsKey("Authorization"))
                    {
                        this.Configuration.DefaultHeader.Add("Authorization", authHeader);
                    }
                    else
                    {
                        this.Configuration.DefaultHeader["Authorization"] = authHeader;
                    }
                    return tokenObj;
                }
                else
                {
                    throw new ApiException((int)response.StatusCode,
                      "Error while requesting server, received a non successful HTTP code "
                      + response.ResponseStatus + " with response Body: " + response.Content, response.Content);
                }
            }

            /// <summary>
            /// Get User Info method takes the accessToken to retrieve User Account Data.
            /// </summary>
            /// <param name="accessToken"></param>
            /// <returns>The User Info model.</returns>
            public OAuth.UserInfo GetUserInfo(string accessToken)
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new ArgumentException("Cannot find a valid access token. Make sure OAuth is configured before you try again.");
                }

                string baseUri = string.Format("https://{0}/", GetOAuthBasePath());

                RestClient restClient = new RestClient(baseUri);
                restClient.Timeout = Configuration.Timeout;
                restClient.UserAgent = Configuration.UserAgent;
                restClient.Proxy = Proxy;

                RestRequest request = new RestRequest("oauth/userinfo", Method.GET);

                request.AddHeader("Authorization", "Bearer " + accessToken);
                // Don't cache authentication requests
                request.AddHeader("Cache-Control", "no-store");
                request.AddHeader("Pragma", "no-cache");

                IRestResponse response = restClient.Execute(request);
                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
                {
                    OAuth.UserInfo userInfo = JsonConvert.DeserializeObject<OAuth.UserInfo>(response.Content);
                    return userInfo;
                }
                else
                {
                    throw new ApiException((int)response.StatusCode,
                          "Error while requesting server, received a non successful HTTP code "
                          + response.ResponseStatus + " with response Body: " + response.Content, response.Content);
                }
            }

            /// <summary>
            /// Creates an RSA Key from the given PEM key.
            /// </summary>
            /// <param name="key"></param>
            /// <returns>RSACryptoServiceProvider using the "key"</returns>
            private static RSA CreateRSAKeyFromPem(string key)
            {
                TextReader reader = new StringReader(key);
                PemReader pemReader = new PemReader(reader);

                object result = pemReader.ReadObject();

                RSA provider = RSA.Create();

                if (result is AsymmetricCipherKeyPair keyPair)
                {
                    var rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)keyPair.Private);
                    provider.ImportParameters(rsaParams);
                    return provider;
                }
                else if (result is RsaKeyParameters keyParameters)
                {
                    var rsaParams = DotNetUtilities.ToRSAParameters(keyParameters);
                    provider.ImportParameters(rsaParams);
                    return provider;
                }

                throw new Exception("Unexpected PEM type");
            }

            /// <summary>
            /// Request JWT User Token
            /// Configures the current instance of ApiClient with a fresh OAuth JWT access token from DocuSign
            /// </summary>
            /// <param name="clientId">DocuSign OAuth Client Id(AKA Integrator Key)</param>
            /// <param name="userId">DocuSign user Id to be impersonated(This is a UUID)</param>
            /// <param name="oauthBasePath"> DocuSign OAuth base path
            /// <see cref="OAuth.Demo_OAuth_BasePath"/> <see cref="OAuth.Production_OAuth_BasePath"/> <see cref="OAuth.Stage_OAuth_BasePath"/>
            /// <seealso cref="GetOAuthBasePath()" /> <seealso cref="SetOAuthBasePath(string)"/>
            /// </param>
            /// <param name="privateKeyStream">The Stream of the RSA private key</param>
            /// <param name="expiresInHours">Number of hours remaining before the JWT assertion is considered as invalid</param>
            /// <param name="scopes">Optional. The list of requested scopes may include (but not limited to)
            /// <see cref="OAuth.Scope_SIGNATURE"/> <see cref="OAuth.Scope_IMPERSONATION"/> <see cref="OAuth.Scope_EXTENDED"/>
            /// </param>
            /// <returns>The JWT user token</returns>
            public OAuth.OAuthToken RequestJWTUserToken(string clientId, string userId, string oauthBasePath, Stream privateKeyStream, int expiresInHours, List<string> scopes = null)
            {
                using (StreamReader sr = new StreamReader(privateKeyStream))
                {
                    if (sr != null && sr.Peek() > 0)
                    {
                        byte[] privateKeyBytes = ReadAsBytes(privateKeyStream);
                        return this.RequestJWTUserToken(clientId, userId, oauthBasePath, privateKeyBytes, expiresInHours, scopes);
                    }
                    else
                    {
                        throw new ApiException(400, "Private key stream not supplied or is invalid!");
                    }
                }
            }

            /// <summary>
            /// Request JWT User Token
            /// Configures the current instance of ApiClient with a fresh OAuth JWT access token from DocuSign
            /// </summary>
            /// <param name="clientId">DocuSign OAuth Client Id(AKA Integrator Key)</param>
            /// <param name="userId">DocuSign user Id to be impersonated(This is a UUID)</param>
            /// <param name="oauthBasePath"> DocuSign OAuth base path
            /// <see cref="OAuth.Demo_OAuth_BasePath"/> <see cref="OAuth.Production_OAuth_BasePath"/> <see cref="OAuth.Stage_OAuth_BasePath"/>
            /// <seealso cref="GetOAuthBasePath()" /> <seealso cref="SetOAuthBasePath(string)"/>
            /// </param>
            /// <param name="privateKeyBytes">The byte contents of the RSA private key</param>
            /// <param name="expiresInHours">Number of hours remaining before the JWT assertion is considered as invalid</param>
            /// <param name="scopes">Optional. The list of requested scopes may include (but not limited to) You can also pass any advanced scope.
            /// <see cref="OAuth.Scope_SIGNATURE"/> <see cref="OAuth.Scope_IMPERSONATION"/> <see cref="OAuth.Scope_EXTENDED"/>
            /// </param>
            /// <returns>The JWT user token</returns>
            public OAuth.OAuthToken RequestJWTUserToken(string clientId, string userId, string oauthBasePath, byte[] privateKeyBytes, int expiresInHours, List<string> scopes = null)
            {
                string privateKey = Encoding.UTF8.GetString(privateKeyBytes);

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler
                {
                    SetDefaultTimesOnTokenCreation = false
                };

                SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
                {
                    Expires = DateTime.UtcNow.AddHours(expiresInHours),
                    IssuedAt = DateTime.UtcNow,
                };

                scopes = scopes ?? new List<string> { OAuth.Scope_SIGNATURE };

                descriptor.Subject = new ClaimsIdentity();
                descriptor.Subject.AddClaim(new Claim("scope", String.Join(" ", scopes)));
                descriptor.Subject.AddClaim(new Claim("aud", oauthBasePath));
                descriptor.Subject.AddClaim(new Claim("iss", clientId));

                if (!string.IsNullOrEmpty(userId))
                {
                    descriptor.Subject.AddClaim(new Claim("sub", userId));
                }
                else
                {
                    throw new ApiException(400, "User Id not supplied or is invalid!");
                }

                if (!string.IsNullOrEmpty(privateKey))
                {
                    var rsa = CreateRSAKeyFromPem(privateKey);
                    RsaSecurityKey rsaKey = new RsaSecurityKey(rsa);
                    descriptor.SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256Signature);
                }
                else
                {
                    throw new ApiException(400, "Private key not supplied or is invalid!");
                }

                var token = handler.CreateToken(descriptor);
                string jwtToken = handler.WriteToken(token);

                string baseUri = string.Format("https://{0}/", oauthBasePath);
                RestClient restClient = new RestClient(baseUri);
                restClient.Timeout = Configuration.Timeout;
                restClient.UserAgent = Configuration.UserAgent;
                restClient.Proxy = Proxy;

                string path = "oauth/token";
                string contentType = "application/x-www-form-urlencoded";

                Dictionary<string, string> formParams = new Dictionary<string, string>();
                formParams.Add("grant_type", OAuth.Grant_Type_JWT);
                formParams.Add("assertion", jwtToken);

                Dictionary<string, string> queryParams = new Dictionary<string, string>();

                Dictionary<string, string> headerParams = new Dictionary<string, string>();
                headerParams.Add("Content-Type", "application/x-www-form-urlencoded");
                // Don't cache authentication requests
                headerParams.Add("Cache-Control", "no-store");
                headerParams.Add("Pragma", "no-cache");

                Dictionary<string, FileParameter> fileParams = new Dictionary<string, FileParameter>();
                Dictionary<string, string> pathParams = new Dictionary<string, string>();

                object postBody = null;

                RestRequest request = PrepareRequest(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, contentType);

                IRestResponse response = restClient.Execute(request);

                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
                {
                    OAuth.OAuthToken tokenInfo = JsonConvert.DeserializeObject<OAuth.OAuthToken>(((RestResponse)response).Content);
                    if (!this.Configuration.DefaultHeader.ContainsKey("Authorization"))
                    {
                        this.Configuration.DefaultHeader.Add("Authorization", string.Format("{0} {1}", tokenInfo.token_type, tokenInfo.access_token));
                    }
                    else
                    {
                        this.Configuration.DefaultHeader["Authorization"] = string.Format("{0} {1}", tokenInfo.token_type, tokenInfo.access_token);
                    }
                    return tokenInfo;
                }
                else
                {
                    throw new ApiException((int)response.StatusCode,
                          "Error while requesting server, received a non successful HTTP code "
                          + response.ResponseStatus + " with response Body: " + response.Content, response.Content);
                }
            }

            /// <summary>
            /// *RESERVED FOR PARTNERS* Request JWT Application Token
            /// </summary>
            /// <param name="clientId">DocuSign OAuth Client Id(AKA Integrator Key)</param>
            /// <param name="oauthBasePath"> DocuSign OAuth base path
            /// <see cref="OAuth.Demo_OAuth_BasePath"/> <see cref="OAuth.Production_OAuth_BasePath"/> <see cref="OAuth.Stage_OAuth_BasePath"/>
            /// <seealso cref="GetOAuthBasePath()" /> <seealso cref="SetOAuthBasePath(string)"/>
            /// </param>
            /// <param name="privateKeyBytes">The byte contents of the RSA private key</param>
            /// <param name="expiresInHours">Number of hours remaining before the JWT assertion is considered as invalid</param>
            /// <param name="scopes">Optional. The list of requested scopes may include (but not limited to) You can also pass any advanced scope.
            /// <see cref="OAuth.Scope_SIGNATURE"/> <see cref="OAuth.Scope_IMPERSONATION"/> <see cref="OAuth.Scope_EXTENDED"/>
            /// </param>
            /// <returns>The JWT application token</returns>
            public OAuth.OAuthToken RequestJWTApplicationToken(string clientId, string oauthBasePath, byte[] privateKeyBytes, int expiresInHours, List<string> scopes = null)
            {
                string privateKey = Encoding.UTF8.GetString(privateKeyBytes);

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

                SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
                {
                    Expires = DateTime.UtcNow.AddHours(expiresInHours),
                };

                scopes = scopes ?? new List<string> { OAuth.Scope_SIGNATURE };

                descriptor.Subject = new ClaimsIdentity();
                descriptor.Subject.AddClaim(new Claim("scope", String.Join(" ", scopes)));
                descriptor.Subject.AddClaim(new Claim("aud", oauthBasePath));
                descriptor.Subject.AddClaim(new Claim("iss", clientId));

                if (!string.IsNullOrEmpty(privateKey))
                {
                    var rsa = CreateRSAKeyFromPem(privateKey);
                    RsaSecurityKey rsaKey = new RsaSecurityKey(rsa);
                    descriptor.SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256Signature);
                }
                else
                {
                    throw new ApiException(400, "Private key not supplied or is invalid!");
                }

                var token = handler.CreateToken(descriptor);
                string jwtToken = handler.WriteToken(token);

                string baseUri = string.Format("https://{0}/", oauthBasePath);
                RestClient restClient = new RestClient(baseUri);
                restClient.Timeout = Configuration.Timeout;
                restClient.UserAgent = Configuration.UserAgent;
                restClient.Proxy = Proxy;

                string path = "oauth/token";
                string contentType = "application/x-www-form-urlencoded";

                Dictionary<string, string> formParams = new Dictionary<string, string>();
                formParams.Add("grant_type", OAuth.Grant_Type_JWT);
                formParams.Add("assertion", jwtToken);

                Dictionary<string, string> queryParams = new Dictionary<string, string>();

                Dictionary<string, string> headerParams = new Dictionary<string, string>();
                headerParams.Add("Content-Type", "application/x-www-form-urlencoded");
                // Don't cache authentication requests
                headerParams.Add("Cache-Control", "no-store");
                headerParams.Add("Pragma", "no-cache");

                Dictionary<string, FileParameter> fileParams = new Dictionary<string, FileParameter>();
                Dictionary<string, string> pathParams = new Dictionary<string, string>();

                object postBody = null;

                RestRequest request = PrepareRequest(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, contentType);

                IRestResponse response = restClient.Execute(request);

                if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest)
                {
                    OAuth.OAuthToken tokenInfo = JsonConvert.DeserializeObject<OAuth.OAuthToken>(((RestResponse)response).Content);
                    if (!this.Configuration.DefaultHeader.ContainsKey("Authorization"))
                    {
                        this.Configuration.DefaultHeader.Add("Authorization", string.Format("{0} {1}", tokenInfo.token_type, tokenInfo.access_token));
                    }
                    else
                    {
                        this.Configuration.DefaultHeader["Authorization"] = string.Format("{0} {1}", tokenInfo.token_type, tokenInfo.access_token);
                    }
                    return tokenInfo;
                }
                else
                {
                    throw new ApiException((int)response.StatusCode,
                          "Error while requesting server, received a non successful HTTP code "
                          + response.ResponseStatus + " with response Body: " + response.Content, response.Content);
                }
            }
        }
    }

