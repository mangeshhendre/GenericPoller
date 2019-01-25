using GenericPoller.Interfaces;
using Microsoft.Practices.Unity;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.SupportClasses
{
    public class AssemblyLocator : IAssemblyLocator
    {
        #region Constants
        const string PollcHandlerBaseTypeName = "PollHandlerBase";
        #endregion

        #region Members
        private Dictionary<string, string> _pollHandlerDirectories;
        #endregion

        #region Constructors
        [InjectionConstructor]
        public AssemblyLocator(string handlerDirectory)
        {
            if (_pollHandlerDirectories == null)
            {
                _pollHandlerDirectories = this.LocateWorkHandlers(handlerDirectory);
            }
        }
        #endregion

        #region Properties
        public Dictionary<string, string> PollHandlerDirectories
        {
            get { return _pollHandlerDirectories; }
        }
        #endregion

        #region Private Methods
        Dictionary<string, string> LocateWorkHandlers(string handlerDirectory)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string subDirectoryPath in Directory.GetDirectories(handlerDirectory))
            {
                foreach (var file in new DirectoryInfo(subDirectoryPath).GetFiles("*.dll"))
                {
                    try
                    {
                        AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(file.FullName);
                        var types = assemblyDefinition.MainModule.GetTypes();

                        foreach (TypeDefinition typeDefinition in types.Where(d =>
                            d.BaseType != null && string.Compare(d.BaseType.Name, PollcHandlerBaseTypeName, true) == 0))
                        {
                            result[typeDefinition.Name] = subDirectoryPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception while attempting to execute AssemblyDefinition.ReadAssembly for: {0}", file.FullName);
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("---------------------------------------------------");
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
