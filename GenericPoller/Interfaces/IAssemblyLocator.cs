using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Interfaces
{
    public interface IAssemblyLocator
    {
        Dictionary<string, string> PollHandlerDirectories { get; }
    }
}
