//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WF2oWFN.Compiler.Frontend;
using System.IO;
using System.Diagnostics;
using WF2oWFN.Compiler.Backend;
using Microsoft.Test.CommandLineParsing;
using System.Xml;
using System.Xaml;
using WF2oWFN.API;
using System.ComponentModel;
using System.Security;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace WF2oWFN
{
    /// <summary>
    /// WF2oWFN Main Program
    /// </summary>
    class Program
    {
        // Globals
        public static String ProgramVersion = "WF2oWFN [Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "]";
        public static String Copyright = "Copyright (c) 2012 Stefan Kolb";
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // Defaults
        private static Format formatFile = Format.owfn;
        private static Boolean debugMode = false;
        private static String inputFile = String.Empty;
        private static String outputFile = String.Empty;

        static void Main(String[] args)
        {
            // CommandLine Parser
            CommandLineArguments arguments = new CommandLineArguments();

            try
            {
                // Parse CommandLine Arguments
                CommandLineParser.ParseArguments(arguments, args);

                // Debug
                if (arguments.d.HasValue)
                {
                    debugMode = true;
                }
                // Format
                if (arguments.f != null && !arguments.f.Equals(String.Empty))
                {
                    try
                    {
                        Format format = (Format)Enum.Parse(typeof(Format), arguments.f);
                        formatFile = format;
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Error: Invalid argument '{0}' for format option (/f).", arguments.f);
                        Console.WriteLine();
                        return;
                    }
                }
                // OutputFile
                if (arguments.o != null && !arguments.o.Equals(String.Empty))
                {
                    try
                    {
                        outputFile = Path.GetFullPath(arguments.o);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Error: Invalid argument '{0}' for output option (/o).", arguments.o);
                        Console.WriteLine();
                        return;
                    }
                }
                // Help
                if (arguments.h.HasValue)
                {
                    PrintHelp();
                    return;
                }
                // Version
                if (arguments.v.HasValue)
                {
                    PrintVersion();
                    return;
                }
                // InputFile
                if (arguments.i != null && !arguments.i.Equals(String.Empty))
                {
                    try
                    {
                        inputFile = Path.GetFullPath(arguments.i);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Error: Invalid argument '{0}' for output option (/i).", arguments.i);
                        Console.WriteLine();
                        return;
                    }
                    inputFile = arguments.i;
                }
                else
                {
                    // Incomplete Commandline
                    throw new ArgumentException();
                }

                // Start Program
                RunProgram();
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Error: Unrecognized or incomplete command line.");
                Console.WriteLine();
                PrintHelp();
            }
        }

        #region Private Methods

        private static void PrintHelp()
        {
            PrintVersion();
            Console.WriteLine();
            Console.WriteLine("Translates WF processes into petri net models.");
            Console.WriteLine();
            Console.WriteLine("Usage: wf2owfn [/i=FILE] [/f=FORMAT] [/o=FILE] [/h] [/v] [/d]");
            Console.WriteLine();
            Console.WriteLine("About:");
            Console.WriteLine("  /h    Print help and exit");
            Console.WriteLine("  /v    Print version and exit");
            Console.WriteLine("  /d    Append debug information");
            Console.WriteLine();
            Console.WriteLine("File input:");
            Console.WriteLine("  /i=FILE      Read a WF process from FILE");
            Console.WriteLine();
            Console.WriteLine("File output:");
            Console.WriteLine("  /f=FORMAT    Create output of the given FORMAT");
            Console.WriteLine("               (possible values='owfn', 'dot', 'png' default='owfn')");
            Console.WriteLine("  /o=FILE      Write output to FILE");
            Console.WriteLine();
        }

        private static void PrintVersion()
        {
            // Version & Copyright
            Console.WriteLine(ProgramVersion);
            Console.WriteLine(Copyright);
            // .NET Version
            Console.WriteLine("You are using .NET Framework version {0}", System.Environment.Version.ToString());
        }

        private static void RunProgram()
        {
            // Scanner
            Scanner scan = null;

            try
            {
                scan = new Scanner(inputFile);
                scan.Scan();
            }
            catch (IOException)
            {
                Console.WriteLine("The specified input file {0} was not found.", inputFile);
                return;
            }
            catch (XmlException e)
            {
                Console.WriteLine("The specified input file is not well-formed Xaml.");
                Console.WriteLine();

                if (debugMode)
                {
                    Console.WriteLine("Debug: {0}", e.Message);
                    Console.WriteLine();
                }
                return;
            }

            // Parser
            IActivity ast = null;

            try
            {
                Parser pars = new Parser(scan.Token());
                // ModuleFactory
                IModuleFactory modules = ModuleFactory.Instance;
                pars.ModuleFactory = modules;
                ast = pars.Parse();
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return;
            }
            catch (ParseException e)
            {
                Console.WriteLine("Error while parsing the Xaml:");
                Console.WriteLine(e.Message);
                if (debugMode)
                {
                    Console.WriteLine("Debug: activity '{0}', queue element near or at '{1}', ", e.Activity, e.ElementNumber);
                    Console.WriteLine();
                }
                return;
            }

            // Generation
            try
            {
                Generation gen = new Generation(ast);
                gen.Compile();

                // Format
                string format = formatFile.ToString();

                if (Path.GetFileNameWithoutExtension(outputFile).Equals(String.Empty))
                {
                    gen.WriteOutput(Path.ChangeExtension(inputFile, format), format);
                    if (File.Exists(Path.ChangeExtension(inputFile, format)))
                        Console.WriteLine("Output file '{0}' sucessfully written", Path.GetFileNameWithoutExtension(inputFile) + "." + format);
                }
                else
                {
                    gen.WriteOutput(Path.ChangeExtension(outputFile, format), format);
                    if (File.Exists(Path.ChangeExtension(outputFile, format)))
                        Console.WriteLine("Output file '{0}' sucessfully written", Path.GetFileNameWithoutExtension(outputFile) + "." + format);
                }
            }
            catch(SecurityException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Win32Exception e)
            {
                Console.WriteLine(e.Message);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected enum Format
        {
            owfn, dot, png
        }

        class CommandLineArguments
        {
            public bool? d { get; set; }
            public bool? h { get; set; }
            public bool? v { get; set; }
            public string i { get; set; }
            public string f { get; set; }
            public string o { get; set; }
        }

        #endregion Private Methods
    }
}
