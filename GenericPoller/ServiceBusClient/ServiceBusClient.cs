using GenericPoller.Client.Async;
using GenericPoller.Client.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client
{
    /// <summary>
    /// Dynamic Service Bus Client
    /// </summary>
    public class ServiceBusClient : DynamicObject
    {
        #region Private Members
        private readonly JsonRPCHttpClient _client;
        private readonly AsyncDynamic _async;
        private readonly AsyncRunnerDynamic _asyncRunner;
        private string _serviceName; 
        #endregion

        #region Constructors
        public ServiceBusClient(string executeUri, string serviceName, TimeSpan? clientTimeout = null)
        {
            _client = new JsonRPCHttpClient(executeUri, clientTimeout);
            _serviceName = serviceName;
            _async = new AsyncDynamic(_client, serviceName);
            _asyncRunner = new AsyncRunnerDynamic();
            _asyncRunner.ServiceBusClient = this;
        } 
        #endregion

        #region Public Methods
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var method = binder.Name;
            var argumentNames = binder.CallInfo.ArgumentNames;

            // accept named args only
            if (argumentNames.Count != args.Length)
            {
                throw new InvalidOperationException("Please use named arguments. Example: myObject.MyMethod(myNamedParameter: 123);");
            }

            //stuff parameters into case insensitive dictionary
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                parameters.Add(argumentNames[i], args[i]);
            }

            //the only way to determine if generic type parameters have been used is reflection
            var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            var typeArgs = csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IList<Type>;
            Type returnType = typeArgs.FirstOrDefault();
          
            //if it exists, the first generic type parameter is our return type (e.g. myObject.MyMethod<int>(Id: 123); --> returnType would be typeof(int))
            if (returnType != null)
            {
                MethodInfo methodInfo = typeof(JsonRPCHttpClient).GetMethod("ExecuteAndConvert");
                MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(returnType);
                try
                {
                    result = genericMethodInfo.Invoke(_client, new object[] { _serviceName, method, parameters });
                }
                catch (TargetInvocationException tiex)
                {
                    throw tiex.InnerException;
                }
            }
            else
            {
                result = _client.Execute(_serviceName, method, parameters);
            }

            return true;
        }
        public object Execute(string method, Dictionary<string, object> parameters)
        {
            return _client.Execute(_serviceName, method, parameters);
        }
        public T Execute<T>(string method, Dictionary<string, object> parameters)
        {
            return _client.ExecuteAndConvert<T>(_serviceName, method, parameters);        
        }
        
        public AsyncRunnerDynamic AsyncRunner(int pollingIntervalMilliseconds = 2000, TimeSpan? pollingTimeout = null)
        {
            _asyncRunner.PollingIntervalMilliseconds = pollingIntervalMilliseconds;
            _asyncRunner.PollingTimeout = pollingTimeout;
            return _asyncRunner;
        }

        public AsyncDynamic AsyncProcess()
        {
            return _async;
        }
        public AsyncResult AsyncResult(string token)
        {
            return _async.GetAsyncResult(token);
        }
        public AsyncResult<T> AsyncResult<T>(string token)
        {
            return _async.GetAsyncResult<T>(token);
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
                this.AsyncProcess().ServiceName = _serviceName;
            }
        }        
        #endregion
    }    
}