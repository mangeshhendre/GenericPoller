using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GenericPoller.Client.Exceptions;
using GenericPoller.Client.Utility;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyLibrary.Common.Json.Support;

namespace GenericPoller.Client.Http
{
    internal class JsonRPCHttpClient
    {
        #region Private Members
        private readonly TimeSpan _defaultTimeout = new TimeSpan(0, 2, 30);
        private readonly JsonUtility _jsonUtility;
        private readonly string _serviceBusExecuteUri;
        private readonly int _clientTimeout;
        //private readonly HttpClient _httpClient; 
        #endregion

        #region Constructors
        public JsonRPCHttpClient(string serviceBusExecuteUri, TimeSpan? clientTimeout = null)
        {
            _jsonUtility = new JsonUtility();
            _serviceBusExecuteUri = serviceBusExecuteUri;
            _clientTimeout = clientTimeout.HasValue ? (int)clientTimeout.Value.TotalMilliseconds : (int)_defaultTimeout.TotalMilliseconds;

            Initialize();
        } 
        #endregion

        #region Public Methods
        public dynamic Execute(string service, string method, Dictionary<string,object> parameters)
        {
            var responseBody = ExecuteRequest(service, method, parameters);

            //in most scenarios we expect success, so we'll deserialize into an ExpandoObject
            var responseExpando = JsonConvert.DeserializeObject<ExpandoObject>(responseBody);

            //just to be safe look for any RpcError
            if (((IDictionary<String, object>)responseExpando).ContainsKey("error"))
            {
                //instead of guesswork with the expando, I want to work with a JsonRpcException
                var jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                var jsonError = jsonResponse.Error;
                throw new RpcException(jsonError.code, jsonError.message, jsonError.data);
            }
            else if (((IDictionary<String, object>)responseExpando).ContainsKey("result"))
            {
                return ((dynamic)responseExpando).result;
            }
            else
            {
                throw new Exception("Unexpected response from the Service Bus");
            }
        }
        public T ExecuteAndConvert<T>(string service, string method, Dictionary<string, object> parameters)
        {
            var responseBody = ExecuteRequest(service, method, parameters);
            var response = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
            if (response.Error != null)
            {
                throw new RpcException(response.Error.code, response.Error.message, response.Error.data);
            }
            else
            {
                return _jsonUtility.ResultToObject<T>(response.Result);
            }
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            try
            {
                //ServicePointManager.Expect100Continue = false;
                //var request = (HttpWebRequest)HttpWebRequest.Create(_serviceBusExecuteUri);
                //request.Method = "POST";
                //request.ContentType = "application/json";
                //request.ContentLength = 1;
                //using (var s = request.GetRequestStream())
                //{
                //    s.Write(new byte[1], 0, 1);
                //}
                //request.GetResponse().Dispose();

                JsonConvert.SerializeObject(new JsonRequest() { JsonRpc = string.Empty, Method = string.Empty, Params = new Dictionary<string, object>(), Id = string.Empty });
            }
            catch { }
        }
        private string ExecuteRequest(string service, string method, Dictionary<string, object> parameters)
        {
            var jsonRequest = _jsonUtility.GetJsonRequest(service, method, parameters);
            var requestString = JsonConvert.SerializeObject(jsonRequest);
            var requestBytes = Encoding.UTF8.GetBytes(requestString);

            try
            {
                //request
                var request = (HttpWebRequest)HttpWebRequest.Create(_serviceBusExecuteUri);
                request.Timeout = _clientTimeout;
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = requestBytes.Length;
                using (var s = request.GetRequestStream())
                {
                    s.Write(requestBytes, 0, requestBytes.Length);
                }

                //response
                string responseBody = null;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidOperationException(string.Format("Invalid Http Status: {0} - {1}", response.StatusCode, response.StatusDescription));

                    using (var s = response.GetResponseStream())
                    {
                        using (var sr = new StreamReader(s, Encoding.Default))
                        {
                            responseBody = sr.ReadToEnd();
                        }
                    }
                }

                return responseBody;
            }
            catch (WebException wex)
            {
                if (wex.Status == WebExceptionStatus.Timeout)
                    throw new GenericPoller.Client.Exceptions.TimeoutException(string.Format("{0} - Timeout: {1} (ms)", GenericPoller.Client.Exceptions.TimeoutException.DefaultExceptionMessage, _clientTimeout));
                else
                    throw wex;
            }
        }
        #endregion
    }
}