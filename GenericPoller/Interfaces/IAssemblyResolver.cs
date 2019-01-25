using MyLibrary.Common.PollHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Interfaces
{
    public interface IAssemblyResolver
    {
        Lazy<PollHandlerBase> GetPollHandler(string handlerDirectory, bool ignoreDuplicates);
    }
}
