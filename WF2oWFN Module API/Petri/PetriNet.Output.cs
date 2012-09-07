﻿//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace WF2oWFN.API.Petri
{
    /// <summary>
    /// Petri Net Outputs
    /// </summary>
    public partial class PetriNet
    {
        // Sets output to FINALCONDITION or FINALMARKING for compatibility with oWFN2BPEL (i.e. false)
        private readonly bool finalCondition = true;
        // Allows/Denies output of PORTS
        private readonly bool outputPorts = false;

        /// <summary>
        /// Outputs the net in oWFN-format.
        /// Compliant to EBNF: Petri Net API 4.02, 28. July 2010, doc p.10
        /// </summary>
        /// <param name="filename">Path to output file</param>
        /// <exception cref="System.NullReferenceException">Thrown when <code>filename</code> is empty.</exception>
        /// <exception cref="System.IO.IOException">Thrown when an error occurs during writing to the output file</exception>
        public void OutputOwfn(String filename) 
        {
            // StreamWriter
            StreamWriter output = null;

            if (filename.Equals(String.Empty)) 
            {
                throw new NullReferenceException("Filename cannot be empty.");
            }

            try
            {
                // StreamWriter
                output = new StreamWriter(new FileStream(filename, FileMode.Create));

                log.Debug("Writing FileHeader.");

                // Write Header
                output.WriteLine("{");
                output.WriteLine("  generated by: WF2oWFN [Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "]");
                output.WriteLine("  net size:     {0}", GetStatistics());
                output.WriteLine("}");
                output.WriteLine();

                log.Debug("Writing Places.");

                // Write Places
                output.WriteLine("PLACE");

                // Write Internal Places
                output.WriteLine("  INTERNAL");
                output.Write("    ");

                for (int i = 0; i < internalPlaces.Count; i++)
                {
                    output.Write(internalPlaces.ElementAt(i).Name);

                    if (i < internalPlaces.Count - 1)
                        output.Write(", ");
                }
                output.WriteLine(";");
                output.WriteLine();

                // Write Input Places
                output.WriteLine("  INPUT");

                int inputCount = 0;
                for (int i = 0; i < inputPlaces.Count; i++)
                {
                    output.Write("    " + inputPlaces.ElementAt(i).Name);

                    if (i < inputPlaces.Count - 1)
                        output.WriteLine(",");

                    inputCount++;
                }
                output.WriteLine(";");
                output.WriteLine();

                // Write Output Places
                output.WriteLine("  OUTPUT");

                for (int i = 0; i < outputPlaces.Count; i++)
                {
                    output.Write("    " + outputPlaces.ElementAt(i).Name);

                    if (i < outputPlaces.Count - 1)
                        output.WriteLine(",");
                }
                output.WriteLine(";");
                output.WriteLine();

                // Ports
                if (outputPorts)
                {
                    log.Debug("Writing Ports.");

                    if (ports.Count != 0)
                    {
                        output.WriteLine();
                        output.WriteLine("PORTS");

                        for (int i = 0; i < ports.Count; i++)
                        {
                            // Elements
                            String key = ports.ElementAt(i).Key;
                            HashSet<Place> value = ports.ElementAt(i).Value;

                            output.WriteLine("  " + key + ":");

                            for (int j = 0; j < value.Count; j++)
                            {
                                if (j != 0)
                                    output.WriteLine(",");

                                output.Write("    " + value.ElementAt(j).Name);
                            }

                            output.WriteLine(";");
                        }

                        output.WriteLine();
                    }
                }

                log.Debug("Writing Initial Marking.");

                // Write Initial Marking
                output.WriteLine();
                output.WriteLine("INITIALMARKING");

                int e = 0;
                for (int i = 0; i < internalPlaces.Count; i++)
                {
                    if (internalPlaces.ElementAt(i).Tokens > 0)
                    {
                        if (e != 0)
                            output.WriteLine(",");
                        output.Write(" " + internalPlaces.ElementAt(i).Name + ":\t" + internalPlaces.ElementAt(i).Tokens);

                        if (internalPlaces.ElementAt(i).History.Contains("0.internal.initial"))
                            output.Write(" {initial place}");
                        e++;
                    }
                }
                output.WriteLine(";");
                output.WriteLine();

                log.Debug("Writing Final Condition/Marking.");

                // Write Final Marking
                if (!finalCondition)
                {
                    output.WriteLine("FINALMARKING");
                    output.Write(" ");

                    // Iterate final set list
                    foreach (HashSet<Place> finalSet in finalSetList)
                    {
                        if (finalSet.Count == 1)
                        {
                            Place place = finalSet.First();
                            output.WriteLine(place.Name + ";");
                        }
                        else
                        {
                            bool first_place = true;

                            for (int i = 0; i < finalSet.Count; i++)
                            {
                                if (!first_place && i != finalSet.Count)
                                    output.Write(", ");
                                output.Write(finalSet.ElementAt(i).Name);
                                first_place = false;
                            }
                            Console.WriteLine(";");
                        }
                    }
                }
                else
                {
                    output.WriteLine("FINALCONDITION");
                    output.Write("  (");

                    // Iterate final set list
                    bool first_set = true;
                    foreach (HashSet<Place> finalSet in finalSetList)
                    {
                        if (!first_set)
                        {
                            output.Write(" OR ");
                        }

                        if (finalSet.Count == 1)
                        {
                            Place place = finalSet.First();
                            output.Write(place.Name + "=1");
                        }
                        else
                        {
                            output.Write("( ");
                            bool first_place = true;

                            for (int i = 0; i < finalSet.Count; i++)
                            {
                                if (!first_place)
                                    output.Write(" AND ");
                                output.Write("(" + finalSet.ElementAt(i).Name + "=1)");
                                first_place = false;
                            }
                            output.Write(")");
                        }
                        first_set = false;
                    }
                    output.Write(")");
                    output.WriteLine(";");
                }
                output.WriteLine();
                output.WriteLine();

                log.Debug("Writing Transitions.");

                // Write Transitions
                foreach (Transition transition in transitions)
                {
                    output.Write("TRANSITION {0}", transition.Name);

                    switch (transition.Type)
                    {
                        case (CommunicationType.Internal):
                            output.Write(output.NewLine);
                            break;
                        case (CommunicationType.Input):
                            output.WriteLine(" { input }");
                            break;
                        case (CommunicationType.Output):
                            output.WriteLine(" { output }");
                            break;
                        case (CommunicationType.InOut):
                            output.WriteLine(" { input/output }");
                            break;
                    }

                    output.Write("  CONSUME ");

                    // PreSet
                    HashSet<Node> preSet = transition.PreSet;
                    for (int i = 0; i < preSet.Count; i++)
                    {
                        // PreNode
                        Node preNode = preSet.ElementAt(i);

                        output.Write(preSet.ElementAt(i).Name);

                        if (GetArcWeight(transition, preNode) != 1)
                            output.Write(":" + GetArcWeight(preNode, transition));

                        if (i != preSet.Count - 1)
                            output.Write(", ");
                    }
                    output.WriteLine(";");

                    output.Write("  PRODUCE ");

                    // PostSet
                    HashSet<Node> postSet = transition.PostSet;
                    for (int i = 0; i < postSet.Count; i++)
                    {
                        // PostNode
                        Node postNode = postSet.ElementAt(i);

                        output.Write(postNode.Name);

                        if (GetArcWeight(transition, postNode) != 1)
                            output.Write(":" + GetArcWeight(transition, postNode));

                        if (i != postSet.Count - 1)
                            output.Write(", ");
                    }

                    output.WriteLine(";");
                    output.WriteLine();
                }
                output.WriteLine();

                log.Debug("Writing EOF.");

                // EOF
                String outputDestinationName;

                if (output.BaseStream is FileStream)
                {
                    FileStream fs = output.BaseStream as FileStream;
                    outputDestinationName = fs.Name;

                    output.WriteLine("{ END OF FILE '" + outputDestinationName + "' }");
                }
                else
                {
                    output.WriteLine("{ END OF FILE }");
                }

                log.Info("oWFN Output written.");
            }
            finally
            {
                // Close Stream
                if (output != null) output.Close();
                log.Debug("StreamWriter closed.");
            }
        }

        /// <summary>
        /// Outputs the net in DOT-format.
        /// Currently uses PNAPI native runtime (4.02 July 2010).
        /// </summary>
        /// <param name="filename">Path to output file</param>
        /// <exception cref="System.NullReferenceException">Thrown when <code>filename</code> is empty.</exception>
        /// <exception cref="System.IO.IOException">Thrown when an error occurs during writing to the temporary oWFN file</exception>
        /// <exception cref="System.Security.SecurityException">Thrown if the user has no rights to access temporary path</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when an error occurs calling PNAPI</exception>
        public void outputDot(String filename)
        {
            if (filename.Equals(String.Empty))
            {
                throw new NullReferenceException("Filename cannot be empty.");
            }

            // Temporary oWFN
            string uniqueFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".owfn";
            OutputOwfn(uniqueFile);

            Process pnapi = new Process();
            pnapi.StartInfo.WorkingDirectory = Environment.CurrentDirectory + "\\Libs";
            pnapi.StartInfo.FileName = Environment.CurrentDirectory + "\\Libs\\petri.exe";
            pnapi.StartInfo.Arguments = uniqueFile + " --output=dot --verbose --removePorts";
            pnapi.StartInfo.UseShellExecute = false;
            pnapi.StartInfo.RedirectStandardOutput = true;

            try
            {
                log.Debug("Writing DOT output...");

                pnapi.Start();
                pnapi.WaitForExit();
                // Copy File
                File.Copy(uniqueFile + ".dot", filename, true);
                File.Delete(uniqueFile + ".dot");
                // Clean Temp
                File.Delete(uniqueFile);

                log.Info("DOT output written.");
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("Error calling PNAPI: " + e.Message);
            }
        }

        /// <summary>
        /// Outputs the net in PNG-Format, if Graphviz was found in the Environment variables
        /// </summary>
        /// <param name="filename">Path to output file</param>
        /// <exception cref="System.NullReferenceException">Thrown when <code>filename</code> is empty.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when an error occurs calling Graphviz DOT</exception>
        /// <exception cref="System.IO.IOException">Thrown when an error occurs during writing to the temporary oWFN file</exception>
        /// <exception cref="System.Security.SecurityException">Thrown if the user has no rights to access temporary path</exception>
        public void outputPng(String filename)
        {
            if (filename.Equals(String.Empty))
            {
                throw new NullReferenceException("Filename cannot be empty.");
            }

            // Temporary DOT
            string uniqueFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".dot";
            outputDot(uniqueFile);

            Process png = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("dot.exe");
            string arg = "-Tpng " + uniqueFile + " -o " + filename;
            startInfo.Arguments = arg;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            png.StartInfo = startInfo;

            try
            {
                log.Debug("Writing PNG output...");

                png.Start();
                png.WaitForExit();

                // Clean Temp
                File.Delete(uniqueFile);

                log.Info("PNG output written.");
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("Error calling Graphviz DOT: " + e.Message);
            }
        }
    }
}