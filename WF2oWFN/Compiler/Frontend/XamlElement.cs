using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using WF2oWFN.API;

namespace WF2oWFN.Compiler.Frontend
{
    /// <summary>
    /// XamlElement representing a Xaml tag
    /// </summary>
    class XamlElement : IXamlElement
    {
        // Local Name
        private String name;
        // Namespace
        private String ns;
        // Attributes
        private IDictionary<String, String> attributes;
        // Closing Element
        private bool isClosing;

        /// <summary>
        /// Constructor
        /// </summary>
        public XamlElement()
        {
            // Initialization
            this.attributes = new Dictionary<String, String>();
        }

        #region Properties 

        /// <summary>
        /// Element's QualifiedName
        /// </summary>
        public String QName
        {
            get { return "{" + this.ns + "}" + this.name; }
        }

        /// <summary>
        /// Element's LocalName
        /// </summary>
        public String LocalName
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Element's Namespace
        /// </summary>
        public String Namespace
        {
            get { return this.ns; }
            set { this.ns = value; }
        }

        /// <summary>
        /// Element's Attributes
        /// </summary>
        public IDictionary<String, String> Attributes
        {
            get { return this.attributes; }
            set { this.attributes = value; }
        }

        /// <summary>
        /// Indicates whether the Element is a closing Tag
        /// </summary>
        public bool IsClosingElement
        {
            get { return this.isClosing; }
            set { this.isClosing = value; }
        }

        #endregion Properties
    }
}