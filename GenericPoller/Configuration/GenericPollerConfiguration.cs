using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Configuration
{
    public class GenericPollerConfiguration
    {
        [InjectionConstructor]
        public GenericPollerConfiguration() { }
        public GenericPollerConfiguration(bool getConfigFromEtcd) { } //provided for unit testing

        public string ServiceBusExecuteEndpoint { get; set; } = "http://localhost:55108/Execute.svc";
        public string PollerDirectory { get; set; } = @"F:\Coding\MyRepos\GenericPoller\PollHandlers";
        public int DefaultSleepMilliseconds { get; set; } = 5000;
        public int AutoShutdownMinutesMin { get; set; } = 2;
        public int AutoShutdownMinutesMax { get; set; } = 5;

        public int LogLevel { get; set; }
    }
}
