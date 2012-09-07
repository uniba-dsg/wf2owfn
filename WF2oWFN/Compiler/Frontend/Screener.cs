//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xaml;
using System.Activities;
using System.Activities.XamlIntegration;
using WF2oWFN.Compiler.Frontend;
using System.IO;
using System.Xml.XPath;
using System.Xml.Linq;

namespace WF2oWFN.Compiler.Frontend
{
    /// <summary>
    /// Removes ViewStateInformation and DataInformation from *.xaml and *.xamlx files.
    /// </summary>
    class Screener
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // Inputfile
        private String inputFile;
        // Outputfile
        private String outputFile;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputFile">The input file containing the Xaml(x) Code</param>
        public Screener(String inputFile)
        {
            this.inputFile = inputFile;
            this.outputFile = inputFile;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputFile">Input file containing the Xaml(x) Code</param>
        /// <param name="outputFile">Output file for the cleaned the Xaml(x) Code</param>
        public Screener(String inputFile, String outputFile)
        {
            this.inputFile = inputFile;
            this.outputFile = outputFile;
        }

        /// <summary>
        /// Removes ViewStateInformation and DataInformation and writes cleaned file to <code>inputFile</code> or optional <code>outputFile</code>
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If no input file is specified</exception>
        /// <exception cref="System.IO.IOException">If an Exception occurs during reading or writing</exception>
        /// <exception cref="System.Xml.XmlException">If the <code>inputFile</code> does not contain valid XML</exception>
        public void ScreenXaml() 
        { 
            if(inputFile.Equals(String.Empty))
            {
                throw new ArgumentNullException("No input file specified.");
            }
            
            RemoveViewStateInformation();
            RemoveDataInformation();
        }

        #region Private Methods

        /// <summary>
        /// Removes the ViewStateInformation of the VS-Designer from the Xaml(x).
        /// </summary>
        private void RemoveViewStateInformation()
        {
            log.Debug("Removing XAML(X) ViewState Information...");

            // Load DOM from File
            XmlDocument doc = new XmlDocument();
            doc.Load(inputFile);
            XPathNavigator navigator = doc.CreateNavigator();
            // Select top node
            navigator.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> nsDictionary = navigator.GetNamespacesInScope(XmlNamespaceScope.All);

            XmlNamespaceManager namespaces = new XmlNamespaceManager(navigator.NameTable);
            // Add all Namespaces
            foreach (KeyValuePair<String, String> ns in nsDictionary.ToList())
            {
                namespaces.AddNamespace(ns.Key, ns.Value);
            }

            // Infos
            String prefix;

            // Remove DesignerInformation
            String expr = "/*[@mc:Ignorable]";

            if (namespaces.HasNamespace("mc"))
            {
                XmlNode att = doc.SelectSingleNode(expr, namespaces);

                if (att != null && !att.Attributes.GetNamedItem("mc:Ignorable").Value.Equals(""))
                {
                    prefix = att.Attributes.GetNamedItem("mc:Ignorable").Value;
                    namespaces.AddNamespace(prefix, "http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation");

                    // Remove Attribute
                    att.Attributes.RemoveNamedItem("mc:Ignorable");
                    // Namespace
                    XmlElement root = doc.DocumentElement;
                    root.Attributes.RemoveNamedItem("xmlns:mc");
                    // ViewStateNamespace
                    root.Attributes.RemoveNamedItem("xmlns:" + prefix);

                    // Elements
                    expr = "//" + prefix + ":*";
                    XmlNodeList nodes = doc.SelectNodes(expr, namespaces);

                    foreach (XmlNode n in nodes)
                    {
                        n.ParentNode.RemoveChild(n);
                    }
                    // Attributes
                    expr = "//*[@" + prefix + ":*]";
                    nodes = doc.SelectNodes(expr, namespaces);

                    foreach (XmlNode n in nodes)
                    {
                        List<String> attributeNames = new List<string>();

                        // Collect (API Workaround)
                        foreach (XmlAttribute attribute in n.Attributes)
                        {
                            if (attribute.NamespaceURI.Equals("http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation"))
                            {
                                attributeNames.Add(attribute.Name);
                            }
                        }
                        // Remove
                        foreach (String name in attributeNames)
                        {
                            n.Attributes.RemoveNamedItem(name);
                        }
                    }
                }
                else
                {
                    log.Debug("Warning: No Xaml(x) Viewstate Information found.");
                }
            }

            doc.Save(outputFile);

            log.Info("Removed XAML(X) ViewState Information.");
        }
            

        /// <summary>
        /// Removes Dataflow-Related Information from the Xaml(x).
        /// This currently includes $.Arguments-tags, $.Variables-tags, Visual Basic Settings (mva), unused clr-Imports and Namespaces.
        /// </summary>
        private void RemoveDataInformation()
        {
            // Namespaces
            IDictionary<string, string> nsDictionary;

            log.Debug("Removing XAML(X) DataInformation.");

            // Load DOM from file
            XmlDocument doc = new XmlDocument();
            doc.Load(outputFile);
            XPathNavigator navigator = doc.CreateNavigator();
            // Select first node
            navigator.MoveToFollowing(XPathNodeType.Element);
            // Namespace dictionary
            nsDictionary = navigator.GetNamespacesInScope(XmlNamespaceScope.All);

            XmlNamespaceManager namespaces = new XmlNamespaceManager(navigator.NameTable);
            // Add all namespaces
            foreach (KeyValuePair<String, String> ns in nsDictionary.ToList())
            {
                namespaces.AddNamespace(ns.Key, ns.Value);   
            }

            // Remove unused clr-Namespaces
            log.Debug("Removing XAML(X) unused clr-Namespaces...");

            foreach (KeyValuePair<String, String> ns in nsDictionary.ToList())
            {
                String xpath;
                bool clean = false;

                if (ns.Value.StartsWith("clr-namespace:") && !ns.Key.Equals(""))
                {
                    xpath = "//" + ns.Key + ":*";
                    XmlNodeList nodes = null;

                    // Only clean if unused
                    nodes = doc.SelectNodes(xpath, namespaces);

                    // Remove Visual Basic Settings
                    if (ns.Value.Equals("clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities"))
                    {
                        clean = true;
                    }
                    else if (nodes.Count == 0)
                    {
                        clean = true;
                    }


                    // Cleanup
                    if (clean)
                    {
                        // Remove Visual Basic Settings Nodes
                        foreach (XmlNode n in nodes)
                        {
                            n.ParentNode.RemoveChild(n);
                        }
                        // Remove Attributes
                        xpath = "//*[@" + ns.Key + ":*]";
                        nodes = doc.SelectNodes(xpath, namespaces);

                        foreach (XmlNode node in nodes)
                        {
                            List<String> attributeNames = new List<string>();

                            // Collect (API Workaround)
                            foreach (XmlAttribute attribute in node.Attributes)
                            {
                                if (attribute.NamespaceURI.Equals(ns.Value))
                                {
                                    attributeNames.Add(attribute.Name);
                                }
                            }
                            // Remove
                            foreach (String name in attributeNames)
                            {
                                node.Attributes.RemoveNamedItem(name);
                            }
                        }
                        // Remove Namespace Declaration
                        XmlElement root = doc.DocumentElement;
                        root.Attributes.RemoveNamedItem("xmlns:" + ns.Key);
                    }
                }
            }

            log.Debug("Removing XAML(X) Arguments...");

            // Remove Members
            String prefix = namespaces.LookupPrefix("http://schemas.microsoft.com/winfx/2006/xaml");
            String arguments;
            XmlNodeList nodeList;

            if (!prefix.Equals(String.Empty))
            {
                arguments = "//" + prefix + ":Members";
                nodeList = doc.SelectNodes(arguments, namespaces);
                foreach (XmlNode n in nodeList)
                {
                    n.ParentNode.RemoveChild(n);
                }
            }

            // Remove Arguments
            arguments = "//*['Argument' = substring(name(), string-length(name())- string-length('Variables') +1)]";
            nodeList = doc.SelectNodes(arguments, namespaces);
            foreach (XmlNode n in nodeList)
            {
                n.ParentNode.RemoveChild(n);
            }

            log.Debug("Removing XAML(X) Variables...");

            // Remove Variables
            String variables = "//*['Variables' = substring(name(), string-length(name())- string-length('Variables') +1)]";
            nodeList = doc.SelectNodes(variables, namespaces);
            foreach (XmlNode n in nodeList)
            {
                n.ParentNode.RemoveChild(n);
            }

            // Save File
            doc.Save(outputFile);

            log.Info("Removed XAML(X) DataInformation.");
        }

        #endregion Private Methods
    }
}