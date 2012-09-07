//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WF2oWFN.API;
using WF2oWFN.API.Petri;

namespace WF2oWFN.Compiler.Backend
{
    /// <summary>
    /// oWFN Generation
    /// </summary>
    class Generation
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // AST activity
        private IActivity ast;
        // Petri Net
        private PetriNet net;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ast">The AST activity</param>
        public Generation(IActivity ast)
        {
            this.ast = ast;
        }

        /// <summary>
        /// Compiles the given AST <code>ast</code> into a oWFN petri net
        /// </summary>
        /// <exception cref="System.NullReferenceException">Thrown when the AST <c>ast</c> is null</exception>
        /// <returns>An equivalent <c>PetriNet</c> representation</returns>
        public PetriNet Compile()
        {
            if (this.ast == null)
            {
                throw new NullReferenceException("AST cannot be null");
            }

            // Phylum
            net = new PetriNet();
            String prefix = net.ActivityCount + ".internal.";
            Place p1 = net.NewPlace(prefix + "initialized");
            Place p2 = net.NewPlace(prefix + "closed");
            // Initial Marking
            p1.Tokens = 1;
            // Final Marking
            HashSet<Place> final = new HashSet<Place>();
            final.Add(p2);
            net.AddFinalSet(final);
            // Compile inner activities
            int newID = ++net.ActivityCount;
            net = ast.Compile(net);
            // Merge final net
            net.Merge(p1, newID + ".internal.initialized");
            net.Merge(p2, newID + ".internal.closed");

            return net;
        }

        /// <summary>
        /// Writes the compiled petri net to a file in a given format
        /// </summary>
        /// <param name="format">The format of the file (currently 'owfn', 'png', 'dot')</param>
        /// <param name="filename">The output filename</param>
        /// <exception cref="System.IO.IOException">Thrown when an error occurs during writing to the output file</exception>
        /// <exception cref="System.Security.SecurityException">Thrown if the user has no rights to access temporary path</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when an error occurs calling external APIs</exception>
        /// <exception cref="System.ArgumentException">Thrown when an unsupported <c>format</c> was supplied</exception>
        public void WriteOutput(String filename, String format)
        {
            switch(format)
            {
                case "owfn":
                    net.OutputOwfn(filename);
                    break;
                case "png":
                    net.outputPng(filename);
                    break;
                case "dot":
                    net.outputDot(filename);
                    break;
                default:
                    throw new ArgumentException("Unknown or unsupported output format");
            }
        }
    }
}
