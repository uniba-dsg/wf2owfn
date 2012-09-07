//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using WF2oWFN.API;

namespace WF2oWFN.Compiler.Frontend
{
    /// <summary>
    /// Loads the modules and creates <c>IActivity</c> instances for parsing and compiling Xaml(x) elements
    /// </summary>
    public sealed class ModuleFactory : IModuleFactory
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Module Directory
        private static readonly String moduleDir = "Modules";
        // Singleton
        private static readonly ModuleFactory instance = new ModuleFactory();
        // Modules
        private IDictionary<String, Type> modules;

        /// <summary>
        /// Private Constructor
        /// </summary>
        /// <exception cref="System.IO.DirectoryNotFoundException">If the module directory was not found</exception>
        private ModuleFactory()
        {
            // Variable Initialization
            modules = new Dictionary<String, Type>();
            // Initialize Factory
            Initialize();
        }

        /// <summary>
        /// Returns the ModuleFactory singleton instance
        /// </summary>
        public static ModuleFactory Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Tries to create an instance of a module for the specified activity
        /// </summary>
        /// <param name="qname">The qualified name of the activity</param>
        /// <returns>An instance of an IActivity module for the given activity or <code>null</code> if no module was found</returns>
        public IActivity CreateActivity(String qname)
        {
            Type asmType = modules[qname];

            if (asmType != null)
            {
                // Initialization
                IActivity module = (IActivity)Activator.CreateInstance(asmType);
                // Dependency Injection
                if (module is IComposite)
                {
                    IComposite composite = (IComposite)module;
                    composite.ModuleFactory = this;
                }
                return module;
            }
            else { return null; }
        }

        #region Private Methods

        private void Initialize()
        {
            // Initialize Module Map
            try
            {
                foreach (String file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, moduleDir), "*.dll"))
                {
                    Assembly asm = Assembly.LoadFile(file);

                    foreach (Type asmType in asm.GetTypes())
                    {
                        if (asmType.GetInterface("IActivity") != null)
                        {
                            IActivity module = (IActivity)Activator.CreateInstance(asmType);
                            try
                            {
                                modules.Add(module.QName, asmType);
                            }
                            catch (ArgumentException)
                            {
                                // Multiple modules for same QName (swallow)
                                log.Warn("ModuleFactory tried to load multiple modules for same QName");
                            }
                            catch (MissingMethodException)
                            {
                                // Probably tried to initiate interface - API DLL in folder?
                                log.Error("ModuleFactory probably tried to initiate an interface type");
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {
                throw new DirectoryNotFoundException("The folder containing the modules could not be found.", e);
            }
            catch(BadImageFormatException e)
            {
                // Ignore illegal files
                log.Info("Module folder contains bad assembly files", e);
            }
        }

        #endregion Private Methods
    }
}
