using MyLibrary.Common.PollHandling;
using MyLibrary.Common.PollHandling.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPollHandler
{
    [Export(typeof(PollHandlerBase))]
    public class Test : PollHandlerBase
    {
        [ImportingConstructor]
        public Test([Import] IPollHandlerToolkit toolkit)
            : base(toolkit)
        {

        }
        public override bool RunOnce()
        {
            Console.WriteLine("Hello Test PollerHandler");
            return true;
        }
    }
}
