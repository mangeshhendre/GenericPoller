using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Client.Async
{
    public class AsyncResult<T>
    {
        public Status Status { get; set; }
        public T Result { get; set; }
    }

    public class AsyncResult
    {
        public Status Status { get; set; }
        public dynamic Result { get; set; }
    }
}
