using GenericPoller.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenericPoller.Client.Async
{
    public class AsyncRunnerDynamic : DynamicObject
    {
        #region Private Members
        //let's default these to something reasonable
        private int _pollingIntervalMilliseconds = 2000;
        private TimeSpan _pollingTimeout = new TimeSpan(0,2,0);

        private readonly MethodInfo _asyncDynamicMethodInfo; 
        #endregion

        #region Constructors
        public AsyncRunnerDynamic()
        {
            var asyncDynamicPublicInstanceMethods = typeof(AsyncDynamic).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            _asyncDynamicMethodInfo = asyncDynamicPublicInstanceMethods.Where(m => m.IsGenericMethod && m.Name.Equals("GetAsyncResult")).First();
        } 
        #endregion

        #region Properties
        public ServiceBusClient ServiceBusClient { get; set; }
        public int PollingIntervalMilliseconds
        {
            get
            {
                return _pollingIntervalMilliseconds;
            }
            set
            {
                _pollingIntervalMilliseconds = value;
            }
        }
        public TimeSpan? PollingTimeout
        {
            get
            {
                return _pollingTimeout;
            }
            set
            {
                _pollingTimeout = value ?? new TimeSpan(0,2,0);
            }
        }
        #endregion

        #region Public Methods
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            //if the polling interval is zero (or less) run this request synchronously
            if (this._pollingIntervalMilliseconds <= 0)
                return this.ServiceBusClient.TryInvokeMember(binder, args, out result);

            result = null;
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

            var token = ServiceBusClient.AsyncProcess().ExecuteAsyncProcess(method, parameters);






            //the only way to determine if generic type parameters have been used is reflection
            var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            var typeArgs = csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IList<Type>;
            Type returnType = typeArgs.FirstOrDefault();

            //if it exists, the first generic type parameter is our return type (e.g. myObject.MyMethod<int>(Id: 123); --> returnType would be typeof(int))
            if (returnType != null)
            {
                MethodInfo genericMethodInfo = _asyncDynamicMethodInfo.MakeGenericMethod(returnType);
                try
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds <= PollingTimeout.Value.TotalMilliseconds)
                    {
                        //async get result
                        var asyncResult = genericMethodInfo.Invoke(ServiceBusClient.AsyncProcess(), new object[] { (string)token });

                        //examine result
                        PropertyInfo statusPropertyInfo = asyncResult.GetType().GetProperty("Status");
                        Status asyncStatus = (Status)statusPropertyInfo.GetValue(asyncResult);
                        if (asyncStatus == Status.Done)
                        {
                            PropertyInfo resultPropertyInfo = asyncResult.GetType().GetProperty("Result");
                            result = resultPropertyInfo.GetValue(asyncResult);
                            return true;
                        }
                        else if (asyncStatus == Status.NotFound)
                        {
                            //todo create a custom exception
                            throw new AsyncNotFoundException();
                        }
                        
                        //wait our polling interval
                        Thread.Sleep(_pollingIntervalMilliseconds);
                    }//end while (not timed out)

                    //if we've made it here... timed out
                    throw new AsyncTimeoutException();
                }
                catch (TargetInvocationException tiex)
                {
                    throw tiex.InnerException;
                }
            }
            else
            {
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds <= PollingTimeout.Value.TotalMilliseconds)
                {
                    //async get result
                    AsyncResult asyncResult = ServiceBusClient.AsyncProcess().GetAsyncResult((string)token) as AsyncResult;

                    //examine result
                    if (asyncResult.Status == Status.Done)
                    {
                        result = asyncResult.Result;
                        return true;
                    }
                    else if (asyncResult.Status == Status.NotFound)
                    {
                        //todo create a custom exception
                        throw new AsyncNotFoundException();
                    }

                    //wait our polling interval
                    Thread.Sleep(_pollingIntervalMilliseconds);
                }//end while (not timed out)

                //if we've made it here... timed out
                throw new AsyncTimeoutException();
            }
        }
        #endregion

    }
}
