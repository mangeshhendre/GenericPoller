using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Exceptions
{
    public class AsyncTimeoutException : Exception
    {
        private const string DEFAULT_EXCEPTION_MESSAGE = "Service Bus asynchronous response has timed out.";

        public AsyncTimeoutException()
            : base(DEFAULT_EXCEPTION_MESSAGE, null)
        {
        }

        public AsyncTimeoutException(string message)
            : base(message, null)
        {
        }
    }
}
