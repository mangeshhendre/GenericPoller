using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection.Emit;
using System.Collections;
using GenericPoller.Interfaces;
using System.Threading;
using GenericPoller.Configuration;
using GenericPoller.Logging;
using GenericPoller.Utility;
using Grpc.Core;
using MyLibrary.Common.PollHandling;
using MyLibrary.Common.Tracing;
using MyLibrary.Common.Logging;

namespace GenericPoller
{
    public class GenericPollerProcess
    {
        #region Private variables
        private const string APPLICATON_NAME = "GenericPoller";

        private readonly IUnityContainer _container;
        private readonly ContextLogger _logger;
        private readonly ContextTracer _tracer;
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly IAssemblyLocator _assemblyLocator;

        private string _className;
        private string _arguments;
        private PollHandlerBase _poller;
        private bool _continueExecution = true;
        private Guid _idGuid;
        private int _pollerSleepMilliseconds;

        private Stopwatch _totalStopwatch;
        private int? _autoShutdownMinutes;

        #endregion

        #region Constructors
        public GenericPollerProcess(IUnityContainer container)
        {
            _container = container;
            _idGuid = Guid.NewGuid();
            _totalStopwatch = new Stopwatch();

            _assemblyLocator = this._container.Resolve<IAssemblyLocator>();
            _assemblyResolver = this._container.Resolve<IAssemblyResolver>();
            _logger = (ContextLogger)this._container.Resolve<ILogger>();
            _tracer = (ContextTracer)this._container.Resolve<ITracer>();

            var config = this._container.Resolve<GenericPollerConfiguration>();
            _pollerSleepMilliseconds = config.DefaultSleepMilliseconds;

            ConfigureAutoShutdown(config);

            this._logger.OtherData["GenericPollerVersion"] = Assembly.GetExecutingAssembly().FullName;
            this._tracer.OtherData["GenericPollerVersion"] = Assembly.GetExecutingAssembly().FullName;
        }
        #endregion

        #region Properties
        /// <summary>
        /// This method should be used ONLY for unit testing.  We want to fully test the GenericPollerProcess, but have to short circuit the POLLING with this property.  
        /// Otherwise it would run forever.
        /// </summary>
        public bool ContinueExecution
        {
            get
            {
                return _continueExecution;
            }
            set
            {
                _continueExecution = value;
            }
        }
        #endregion

        #region Event Handlers
        #endregion

        #region Public and Protected Methods
        public void Run(string className, string[] args)
        {
            var arguments = args == null ? null : string.Join(" ", args);
            _logger.ApplicationName = APPLICATON_NAME;
            _logger.ApplicationParameters = string.Format("{0} {1}", className, arguments).Trim();
            _logger.ContextPrimary = className;
            _logger.ContextSecondary = "RunOnce";
            _logger.OtherData["InstanceId"] = _idGuid.ToString();
            _tracer.ApplicationName = APPLICATON_NAME;
            _tracer.ApplicationParameters = string.Format("{0} {1}", className, arguments).Trim();
            _tracer.ContextPrimary = className;
            _tracer.ContextSecondary = "RunOnce";
            _tracer.OtherData["InstanceId"] = _idGuid.ToString();


            //Validate and set what type of GenericPoller this is
            if (!this.ValidateClassName(className)) return;
            _arguments = arguments;

            //Create the handler instance
            CreatePollHandler(args);

            //Trace
            Console.WriteLine(string.Concat(_idGuid, " ready to poll."));
            _tracer.Write(string.Format("Poller started - Class: {0} - Id: {1}", className, _idGuid));

            //Begin polling
            while (this._continueExecution)
            {
                this.Poll();

                this.Wait();

                this.CheckAutoShutdown();
            } 
        }
        #endregion

        #region Event Handlers
        void _poller_ConfigChanged(object sender, PollHandlerConfigChangedEventArgs e)
        {
            _pollerSleepMilliseconds = e.PollingSleepMilliseconds;
        }
        #endregion

        #region Private Methods
        private void Poll()
        {
            try
            {                
                _poller.RunOnce();
            }
            catch (Exception ex)
            {
                var loggerExceptionDict = GetExceptionDictionary(ex);
                this._logger.Error(ex.Message, "GenericPollerProcess", "Poll", ex, loggerExceptionDict);

                var errorString = new StringBuilder();
                foreach (var item in loggerExceptionDict)
                {
                    errorString.AppendLine(string.Format("{0} --- {1}", item.Key, item.Value));
                }

                Console.WriteLine(errorString.ToString());
            }
        }
        private void CheckAutoShutdown()
        {
            if (_autoShutdownMinutes != null && _totalStopwatch.Elapsed.TotalMinutes >= _autoShutdownMinutes)
            {
                _logger.Info(string.Format("GenericPoller {0} - Auto Shutdown at {1} Minutes", _className, _autoShutdownMinutes), "GenericPollerProcess", "CheckAutoShutdown");
                System.Threading.Thread.Sleep(2000); //Logging happens async, sleep to make sure it completes before returning and shutting the app down
                this._continueExecution = false;
            }
        }
        private void Wait()
        {
            Thread.Sleep(_pollerSleepMilliseconds);
        }
        private bool ValidateClassName(string className)
        {
            _className = className;

            var valid = _assemblyLocator.PollHandlerDirectories.ContainsKey(_className);
            if (!valid)
            {
                _logger.Error(string.Format("GenericPollerProcess Unable to find PollHandler for {0}", _className), "GenericPollerProcess", "ValidateClassName");
            }
            return valid;
        }
        private void CreatePollHandler(string[] args)
        {
            var pollerDirectory = _assemblyLocator.PollHandlerDirectories[_className];
            var lazyPoller = _assemblyResolver.GetPollHandler(pollerDirectory, false);
            _poller = lazyPoller.Value as PollHandlerBase;
            _poller.ConfigChanged += _poller_ConfigChanged;
            _poller.Toolkit.CommandLineArgs = args;

            var pollHandlerVersion = _poller.GetType().Assembly.GetName().ToString();
            _logger.OtherData["PollHandlerVersion"] = pollHandlerVersion;
            _tracer.OtherData["PollHandlerVersion"] = pollHandlerVersion;
            _logger.OtherData["PollerName"] = _className;
            _tracer.OtherData["PollerName"] = _className;

            //used for backward compatibility
            AddArgumentsToPollHandlerConfig(args);

            _totalStopwatch.Start();
        }
        private void ConfigureAutoShutdown(GenericPollerConfiguration config)
        {
            this._totalStopwatch = new Stopwatch();
            _autoShutdownMinutes = new Random().Next((int)config.AutoShutdownMinutesMin, (int)config.AutoShutdownMinutesMax);
            if (_autoShutdownMinutes == 0) _autoShutdownMinutes = null;
        }
        private Dictionary<string, string> GetExceptionDictionary(Exception ex)
        {
            var ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ret["exceptionmessage"] = ex.Message;
            ret["exceptionstacktrace"] = ex.StackTrace;
            if (ex.TargetSite != null)
                ret["exceptiontargetsummary"] = string.Format("{0} {1} from {2}", ex.TargetSite.MemberType, ex.TargetSite.Name, ex.TargetSite.ReflectedType.FullName);

            if (ex.InnerException != null)
            {
                ret["innerexceptionmessage"] = ex.InnerException.Message;
                ret["innerexceptionstacktrace"] = ex.InnerException.StackTrace;
                if (ex.InnerException.TargetSite != null)
                    ret["innerexceptiontargetsummary"] = string.Format("{0} {1} from {2}", ex.InnerException.TargetSite.MemberType, ex.InnerException.TargetSite.Name, ex.InnerException.TargetSite.ReflectedType.FullName);

            }

            return ret;
        }
        private void AddArgumentsToPollHandlerConfig(string[] args)
        {
            //if arguments exist they will be added to the PollHandler's Configuration in a section "GenericPollerArgs"
            //we don't know what arguments could be required in the future or what they will be used for, so keep it vague
            //arg0 arg1 arg2 etc
            if (args != null && args.Length > 0)
            {
                var genericPollerArgsAppSettingsSection = new AppSettingsSection();
                for (int i = 0; i < args.Length; i++)
                {
                    genericPollerArgsAppSettingsSection.Settings.Add("arg" + i, args[i]);
                }

                if (_poller.Toolkit.ConfigReader.Sections != null)
                    _poller.Toolkit.ConfigReader.Sections.Add("GenericPollerArgs", genericPollerArgsAppSettingsSection);
            }
        }

        #endregion
    }
}
