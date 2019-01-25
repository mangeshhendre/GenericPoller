using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Exceptions
{
    public class AsyncNotFoundException : Exception
    {
        private const string DEFAULT_EXCEPTION_MESSAGE = "Service Bus asynchronous message not found.";

        public AsyncNotFoundException()
            : base(DEFAULT_EXCEPTION_MESSAGE, null)
        {
        }

        public AsyncNotFoundException(string message)
            : base(message, null)
        {
        }
    }
}
