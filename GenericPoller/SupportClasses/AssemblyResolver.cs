using GenericPoller.Interfaces;
using MyLibrary.Common.PollHandling;
using GenericPoller.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MyLibrary.Common.PollHandling.Interfaces;
using MyLibrary.Common.Logging;

namespace GenericPoller.SupportClasses
{
    public class AssemblyResolver : IAssemblyResolver
    {
        private CompositionContainer _container;
        private IPollHandlerToolkit _toolkit;
        private ContextLogger _logger;

        public AssemblyResolver(IPollHandlerToolkit toolkit, ILogger logger)
        {
            this._toolkit = toolkit;
            this._logger = (ContextLogger)logger;
        }

        [ImportMany]
        protected IEnumerable<Lazy<PollHandlerBase>> Pollers;

        public Lazy<PollHandlerBase> GetPollHandler(string pollerDirectory, bool ignoreDuplicates)
        {
            try
            {
                //Create an AggregateCatalog and recursively add DirectoryCatalogs
                AggregateCatalog aggregateCatalog = new AggregateCatalog();
                this.AddDirectoryToCatalog(aggregateCatalog, pollerDirectory);

                this._container = new CompositionContainer(aggregateCatalog);
                this._container.ComposeExportedValue<IPollHandlerToolkit>(this._toolkit);

                this._container.ComposeParts(this);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Exception loadEx in ex.LoaderExceptions)
                {
                    Console.WriteLine("Loader exception: " + loadEx.Message);
                }
                throw;
            }


            return Pollers.FirstOrDefault();
        }

        #region Private Methods
        private void AddDirectoryToCatalog(AggregateCatalog catalog, string directoryPath)
        {
            var directoryCatalog = new DirectoryCatalog(directoryPath);
            try
            {
                if (directoryCatalog.Parts.ToArray().Count() > 0) // throws ReflectionTypeLoadException on bad assembly
                {
                    catalog.Catalogs.Add(directoryCatalog);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine(ex.Message);
                _logger.Error(string.Format("GenericPoller AssemblyResolver - {0} - {1}", directoryPath, ex.Message), "GenericPoller.AssemblyResolver", "AddDirectoryToCatalog", ex);

                foreach (Exception loaderEx in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderEx.Message);
                    _logger.Error(loaderEx.Message, "GenericPoller.AssemblyResolver", "AddDirectoryToCatalog", loaderEx);
                }
            }

            foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath))
            {
                this.AddDirectoryToCatalog(catalog, subDirectoryPath);
            }
        }
        #endregion
    }
}
