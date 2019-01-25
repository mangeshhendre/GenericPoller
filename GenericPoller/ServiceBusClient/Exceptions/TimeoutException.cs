using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Exceptions
{
    public class TimeoutException : Exception
    {
        public static string DefaultExceptionMessage = "Service Bus client timed out. ";

        public TimeoutException()
            : base(DefaultExceptionMessage, null)
        {
        }

        public TimeoutException(string message)
            : base(message, null)
        {
        }
    }
}