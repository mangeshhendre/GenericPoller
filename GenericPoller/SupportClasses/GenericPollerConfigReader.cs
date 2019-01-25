using GenericPoller.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.SupportClasses
{
    public class GenericPollerConfigReader : IGenericPollerConfigReader
    {
        #region Private Members
        private const string APP_SETTINGS_SHADOW_COPY_POLL_HANDLERS = "ShadowCopyPollHandlers";
        private bool _shadowCopyPollHandlers = true;
        #endregion

        #region Constructors
        public GenericPollerConfigReader()
        {
            LoadFromAppConfig();
        }
        #endregion

        #region Properties
        public bool ShadowCopyPollHandlers
        {
            get
            {
                return _shadowCopyPollHandlers;
            }
            set
            {
                _shadowCopyPollHandlers = value;
            }
        }
        #endregion

        #region Private Methods
        private void LoadFromAppConfig()
        {
            var shadowCopyWorkHandlers = ConfigurationManager.AppSettings[APP_SETTINGS_SHADOW_COPY_POLL_HANDLERS];
            if (shadowCopyWorkHandlers != null) this.ShadowCopyPollHandlers = Convert.ToBoolean(shadowCopyWorkHandlers);
        }
        #endregion
    }
}
