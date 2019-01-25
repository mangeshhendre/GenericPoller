using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Exceptions
{
    public class RpcException : Exception
    {
        private const string DEFAULT_EXCEPTION_MESSAGE = "Service Bus method has thrown an exception.";
        private readonly int _code;
        private readonly object _data;

        public RpcException(int code, string message, object data)
            : base(message, null)
        {
            _code = code;
            _data = data;
        }

        public int Code { get { return _code; } }
        public object RpcData { get { return _data; } }
    }
}