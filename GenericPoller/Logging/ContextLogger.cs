using Microsoft.Practices.Unity;
using MyLibrary.Common.Logging;
using GenericPoller.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Logging
{
    class ContextLogger : ILogger
    {
        #region Private Members
        private readonly dynamic _loggingServiceBusClient;
        private readonly GenericPollerConfiguration _genericPollerConfiguration;
        private LogEntrySeverity _logLevel = LogEntrySeverity.Error;
        private string _applicationName = null;
        private string _applicationParameters = null;
        private string _contextPrimary = null;
        private string _contextSecondary = null;
        private Dictionary<string, string> _otherData = new Dictionary<string, string>();
        #endregion

        #region Constructors
        public ContextLogger([Dependency("LoggingServiceBusClient")]dynamic loggingServiceBusClient, GenericPollerConfiguration genericPollerConfiguration)
        {
            _loggingServiceBusClient = loggingServiceBusClient;
            _genericPollerConfiguration = genericPollerConfiguration;
            _logLevel = (LogEntrySeverity)_genericPollerConfiguration.LogLevel;
        }
        #endregion

        #region Public Properties
        public LogEntrySeverity LogLevel { get { return _logLevel; } set { _logLevel = value; } }
        public string ApplicationName { get { return _applicationName; } set { _applicationName = value; } }
        public string ApplicationParameters { get { return _applicationParameters; } set { _applicationParameters = value; } }
        public string ContextPrimary { get { return _contextPrimary; } set { _contextPrimary = value; } }
        public string ContextSecondary { get { return _contextSecondary; } set { _contextSecondary = value; } }
        public Dictionary<string, string> OtherData { get { return _otherData; } set { _otherData = value; } }
        #endregion

        #region Public Methods
        public void Error(string message, Exception ex = null, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Error, message, _contextPrimary, _contextSecondary, ex, otherData);
        }

        public void Error(string message, string contextPrimary, string contextSecondary, Exception ex = null, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Error, message, contextPrimary, contextSecondary, ex, otherData);
        }

        public void Warn(string message, Exception ex = null, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Warn, message, _contextPrimary, _contextSecondary, ex, otherData);
        }

        public void Warn(string message, string contextPrimary, string contextSecondary, Exception ex = null, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Warn, message, contextPrimary, contextSecondary, ex, otherData);
        }

        public void Debug(string message, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Debug, message, _contextPrimary, _contextSecondary, otherData: otherData);
        }

        public void Debug(string message, string contextPrimary, string contextSecondary, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Debug, message, contextPrimary, contextSecondary, otherData: otherData);
        }

        public void Info(string message, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Info, message, _contextPrimary, _contextSecondary, otherData: otherData);
        }

        public void Info(string message, string contextPrimary, string contextSecondary, Dictionary<string, string> otherData = null)
        {
            this.Put(LogEntrySeverity.Info, message, contextPrimary, contextSecondary, otherData: otherData);
        }
        #endregion

        #region Private Methods
        private void Put(LogEntrySeverity level, string message, string contextPrimary, string contextSecondary, Exception ex = null, Dictionary<string, string> otherData = null)
        {
            //only log if the current desired level calls for it
            if (_logLevel < level)
                return;

            //add exception details to otherData (if they don't already exist)
            AddExceptionToOtherData(ex, ref otherData);

            //add "global" OtherData to otherData
            AddGlobalOtherDataToOtherData(this.OtherData, ref otherData);

            //send it
            Task.Factory.StartNew(() =>
            {
                var loggingWriteResponse = _loggingServiceBusClient.Write(
                        Host: Environment.MachineName,
                        Application: _applicationName,
                        ApplicationParameters: _applicationParameters,
                        PID: Process.GetCurrentProcess().Id,
                        Level: level,
                        Message: message,
                        Language: ".NET",
                        ContextPrimary: contextPrimary,
                        ContextSecondary: contextSecondary,
                        Other: otherData
                    );

                if (!loggingWriteResponse.Success)
                {
                    Console.Write(message);
                }
            }); //task
        }
        private void AddExceptionToOtherData(Exception ex, ref Dictionary<string, string> otherData)
        {
            if (ex == null)
                return;

            var newOtherData = otherData == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(otherData, StringComparer.OrdinalIgnoreCase);

            if (!newOtherData.ContainsKey("ExceptionMessage"))
                newOtherData["ExceptionMessage"] = ex.Message;

            if (!newOtherData.ContainsKey("ExceptionStackTrace"))
                newOtherData["ExceptionStackTrace"] = ex.StackTrace;

            if (!newOtherData.ContainsKey("InnerExceptionMessage") && ex.InnerException != null)
                newOtherData["InnerExceptionMessage"] = ex.InnerException.Message;

            otherData = newOtherData;
        }

        private void AddGlobalOtherDataToOtherData(Dictionary<string, string> globalOtherData, ref Dictionary<string, string> otherData)
        {
            var newOtherData = otherData == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(otherData, StringComparer.OrdinalIgnoreCase);

            //add extra to other 
            foreach (var de in globalOtherData)
            {
                if (!newOtherData.ContainsKey(de.Key))
                    newOtherData[de.Key] = de.Value;
            }

            otherData = newOtherData;
        }
        #endregion
    }
}
