using Newtonsoft.Json;
using GenericPoller.Client.Exceptions;
using GenericPoller.Client.Http;
using GenericPoller.Client.Utility;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLibrary.Common.Json.Support;

namespace GenericPoller.Client.Async
{
    public class AsyncDynamic : DynamicObject
    {
        #region Private Members
        private const string AsyncServiceName = "SBAsync";
        private const string AsyncProcessMethod = "Process";
        private const string AsyncResultMethod = "GetResult";

        private readonly JsonUtility _jsonUtility;
        private readonly JsonRPCHttpClient _client;
        private string _serviceName; 
        #endregion

        #region Constructors
        private AsyncDynamic() { }
        internal AsyncDynamic(JsonRPCHttpClient client, string serviceName)
        {
            _jsonUtility = new JsonUtility();
            _client = client;
            _serviceName = serviceName;
        } 
        #endregion

        #region Public Methods
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var info = binder.CallInfo;
            var method = binder.Name;

            // accepting named args only... SKEET!
            if (info.ArgumentNames.Count != args.Length)
            {
                throw new InvalidOperationException("Please use named arguments.");
            }

            //stuff parameters into case insensitive dictionary
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                parameters.Add(info.ArgumentNames[i], args[i]);
            }

            var request = _jsonUtility.GetJsonRequest(_serviceName, method, parameters);
            result = _client.Execute(AsyncServiceName, AsyncProcessMethod, new Dictionary<string, object> { { "request", request } });


            return true;
        }

        public AsyncResult GetAsyncResult(string token)
        {
            var asyncResult = this.ExecuteAndGetAsyncResult<ExpandoObject>(token);

            return new AsyncResult
            {
                Status = asyncResult.Status,
                Result = asyncResult.Result
            };
        }

        public AsyncResult<T> GetAsyncResult<T>(string token)
        {
            return this.ExecuteAndGetAsyncResult<T>(token);
        }




        internal object ExecuteAsyncProcess(string method, Dictionary<string, object> parameters)
        {
            var request = _jsonUtility.GetJsonRequest(_serviceName, method, parameters);
            return _client.Execute(AsyncServiceName, AsyncProcessMethod, new Dictionary<string, object> { { "request", request } });
        }        
        #endregion   
     
        #region Public Properties
        public string ServiceName
        {
            get
            {
                return _serviceName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new InvalidOperationException("ServiceName must not be NULL or EMPTY");

                _serviceName = value;
            }
        }
        #endregion

        #region Private Methods
        private AsyncResult<T> ExecuteAndGetAsyncResult<T>(string token)
        {
            var result = _client.Execute(AsyncServiceName, AsyncResultMethod, new Dictionary<string, object> { { "id", token } });
            bool isStatusDone = ((string)result.Status).Equals(Status.Done.ToString(), StringComparison.OrdinalIgnoreCase);

            var returnValue = new AsyncResult<T>
            {
                Status = (Status)Enum.Parse(typeof(Status), (string)result.Status, true),
                Result = default(T)
            };

            if (result.AsyncResult != null)
            {
                JsonResponse jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(result.AsyncResult);

                if (jsonResponse.Error != null)
                {
                    throw new RpcException(jsonResponse.Error.code, jsonResponse.Error.message, jsonResponse.Error.data);
                }
                else if (jsonResponse.Result != null)
                {
                    returnValue.Result = _jsonUtility.ResultToObject<T>(jsonResponse.Result);
                }
                else
                {
                    throw new Exception("Unexpected response from the Service Bus");
                }
            }

            return returnValue;
        } 
        #endregion
    }
}
