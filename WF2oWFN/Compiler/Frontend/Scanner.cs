//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Xaml;
using System.Activities;
using System.Activities.XamlIntegration;
using System.IO;
using System.ServiceModel;
using WF2oWFN.API;

namespace WF2oWFN.Compiler.Frontend
{
    /// <summary>
    /// Scanner
    /// </summary>
    class Scanner
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // Inputfile
        private String inputFile;
        // Token Queue
        private Queue<IXamlElement> tokens = new Queue<IXamlElement>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputFile">The file containing the XAML</param>
        public Scanner(String inputFile)
        {
            this.inputFile = inputFile;
        }

        /// <summary>
        /// Transforms the xml data of the <code>inputFile</code> to a token queue
        /// </summary>
        /// <exception cref="System.Xml.XmlException">If the Xaml is not well-formed XML</exception>
        /// <exception cref="System.IO.FileNotFoundException">If the <code>inputFile</code> was not found</exception>
        /// <returns>The token queue generated from the <code>inputFile</code></returns>
        public Queue<IXamlElement> Scan()
        {
            // Validate Xml
            ValidateXml();
            // Try to validate Xaml
            ValidateXaml();

            // Screener
            Screener screener = new Screener(inputFile);
            screener.ScreenXaml();

            // Scan tokens
            XmlTextReader reader = new XmlTextReader(inputFile);
            // Current token
            IXamlElement element = null;
            String name;
            String ns;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        // Enqueue last element
                        if (element != null)
                        {
                            tokens.Enqueue(element);
                        }

                        // Element
                        name = reader.LocalName;
                        ns = reader.NamespaceURI;
                        element = new XamlElement();
                        element.Namespace = ns;
                        element.LocalName = name;
                        bool empty = reader.IsEmptyElement;

                        // Attributes
                        if (reader.HasAttributes)
                        {
                            IDictionary<string, string> dic = element.Attributes;

                            while (reader.MoveToNextAttribute())
                            {
                                // Add Attributes
                                String value = reader.Value;
                                String attname = reader.Name;
                                dic.Add(attname, value);
                            }

                            element.Attributes = dic;

                            if (empty)
                            {
                                tokens.Enqueue(element);
                            }
                        }
                        else
                        {
                            if (empty)
                            {
                                tokens.Enqueue(element);
                            }
                        }

                        // Closing Element
                        if (empty)
                        {
                            element = new XamlElement();
                            element.Namespace = ns;
                            element.LocalName = name;
                            element.IsClosingElement = true;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        // Enqueue last element
                        if (element != null)
                        {
                            tokens.Enqueue(element);
                        }

                        name = reader.LocalName;
                        ns = reader.NamespaceURI;
                        element = new XamlElement();
                        element.Namespace = ns;
                        element.LocalName = name;
                        element.IsClosingElement = true;
                        break;

                    case XmlNodeType.Text:
                        // Add inner text as self-titled attribute
                        // e.g. <x:Reference>__REFERENCEID1</x:Reference> -> Reference="__REFERENCEID1"
                        String inner = reader.Value;
                        element.Attributes.Add(element.LocalName, inner);
                        break;

                    default:
                        // Nothing to handle
                        break;
                }
            }
            // Last Element
            tokens.Enqueue(element);

            // Cleanup
            reader.Close();

            return tokens;
        }

        /// <summary>
        /// Returns the generated token queue
        /// </summary>
        /// <returns>The token queue generated from the <code>inputFile</code></returns>
        public Queue<IXamlElement> Token()
        {
            return tokens;
        }

        #region Private Methods

        /// <summary>
        /// Validates well-formedness of the Xml
        /// </summary>
        /// <exception cref="System.Xml.XmlException">If the Xml is not wellformed</exception>
        /// <exception cref="System.IO.FileNotFoundException">If the <code>inputFile</code> was not found</exception>
        private void ValidateXml()
        {
            XmlTextReader reader = new XmlTextReader(inputFile);

            try
            {
                while (reader.Read()) { }

                log.Info("XML successfully validated.");
            }
            finally
            {
                // Cleanup
                if (reader != null) reader.Close();
            }
        }

        /// <summary>
        /// Validates well-formedness of the Xaml
        /// </summary>
        /// <exception cref="System.IO.FileNotFoundException">If the <code>inputFile</code> was not found</exception>
        private void ValidateXaml()
        {
            XmlReader xmlReader = null;
            XamlXmlReader xamlReader = null;

            try
            {
                log.Debug("Validating XAML.");

                xmlReader = XmlReader.Create(inputFile);
                xamlReader = new XamlXmlReader(xmlReader);
                ActivityBuilder activity = XamlServices.Load(ActivityXamlServices.CreateBuilderReader(xamlReader)) as ActivityBuilder;

                log.Info("XAML successfully validated.");
            }
            catch (XamlException)
            {
                // Only notify user
                log.Warn("XAML could not be validated. This may also be caused by Custom Activities or WorkflowServices.");
            }
            finally
            {
                // Cleanup
                if (xmlReader != null) xmlReader.Close();
                if (xamlReader != null) xamlReader.Close();
            }
        }

        #endregion Private Methods
    }
}
