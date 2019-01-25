using GenericPoller.Client;
using GenericPoller.Configuration;
using GenericPoller.Interfaces;
using GenericPoller.Logging;
using GenericPoller.SupportClasses;
using Microsoft.Practices.Unity;
using MyLibrary.Common.Json;
using MyLibrary.Common.Json.Interfaces;
using MyLibrary.Common.Logging;
using MyLibrary.Common.PollHandling;
using MyLibrary.Common.PollHandling.Interfaces;
using MyLibrary.Common.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: GenericPoller.exe ClassName [Argument1] [Argument2] [Argument3] ...");
                return;
            }

            //load command arguments
            var className = args[0];
            var arguments = args.Length == 1 ? null : args.Skip(1).ToArray();
            Console.WriteLine("Starting: {0} {1}", className, arguments == null ? null : string.Join(" ", arguments));

            //some things need to be in the config file itself and loaded outside of the container
            var configReader = new GenericPollerConfigReader();
            var shadowCopy = configReader.ShadowCopyPollHandlers;
            Console.WriteLine("Shadow Copy: {0}", shadowCopy);

            StartPollerProcess(className, arguments, shadowCopy);
        }

        #region Private Members
        private static void StartPollerProcess(string className, string[] arguments, bool shadowCopy)
        {
            if (shadowCopy)
            {
                //start in new appDomain
                var appDomainSetup = new AppDomainSetup { ShadowCopyFiles = "true" };
                var appDomain = AppDomain.CreateDomain(AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.Evidence, appDomainSetup);
                appDomain.SetData("ClassName", className);
                appDomain.SetData("Arguments", arguments);
                appDomain.DoCallBack(new CrossAppDomainDelegate(DoWorkInShadowCopiedDomain));
            }
            else
            {
                //start in host appDomain
                var process = new GenericPollerProcess(CreateUnityContainer());
                GenericPollerProcessRun(process, className, arguments);
            }
        }

        private static void DoWorkInShadowCopiedDomain()
        {
            var className = (string)AppDomain.CurrentDomain.GetData("ClassName");
            var arguments = (string[])AppDomain.CurrentDomain.GetData("Arguments");

            GenericPollerProcess process = new GenericPollerProcess(CreateUnityContainer());

            //execute
            GenericPollerProcessRun(process, className, arguments);
        }

        private static void GenericPollerProcessRun(GenericPollerProcess process, string className, string[] arguments)
        {
            try
            {
                process.Run(className, arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception: {0}", ex.Message));
                Console.WriteLine(string.Format("StackTrace: {0}", ex.StackTrace));
            }
        }

        private static IUnityContainer CreateUnityContainer()
        {
            var genericPollerConfiguration = new GenericPollerConfiguration();

            UnityContainer container = new UnityContainer();
			
            container.RegisterInstance<GenericPollerConfiguration>(genericPollerConfiguration);
            container.RegisterType<IAssemblyResolver, AssemblyResolver>(new ContainerControlledLifetimeManager());
            container.RegisterType<IJSonRPCHelper, JSonRPCHelper>();
            container.RegisterType<IPollHandlerToolkit, PollHandlerToolkit>();
            //container.RegisterType<ILogEntryBuilder, LogEntryBuilder>(new InjectionConstructor(applicationName));
            //container.RegisterType<ILogger, Logger>();
            container.RegisterType<ILogger, ContextLogger>(new ContainerControlledLifetimeManager());
            container.RegisterType<ITracer, ContextTracer>(new ContainerControlledLifetimeManager());
            container.RegisterType<IAssemblyLocator, AssemblyLocator>(new InjectionConstructor(genericPollerConfiguration.PollerDirectory));
            container.RegisterInstance<dynamic>("LoggingServiceBusClient", new ServiceBusClient(genericPollerConfiguration.ServiceBusExecuteEndpoint, "Logging"));
            return container;
        }
        #endregion

        
    }
}
