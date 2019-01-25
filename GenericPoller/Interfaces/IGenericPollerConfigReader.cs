using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Interfaces
{
    public interface IGenericPollerConfigReader
    {
        bool ShadowCopyPollHandlers { get; set; }
    }
}
