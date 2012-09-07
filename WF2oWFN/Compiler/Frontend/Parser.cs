//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WF2oWFN.Compiler;
using System.IO;
using System.Reflection;
using WF2oWFN.Compiler.Frontend;
using System.Xaml;
using WF2oWFN.API;

namespace WF2oWFN.Compiler.Frontend
{
    /// <summary>
    /// Parser
    /// </summary>
    class Parser
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Tokens Queue
        private Queue<IXamlElement> tokens;
        private int initialTokenCount;
        // ModuleFactory
        private IModuleFactory moduleFactory;
        // Root Tags
        private readonly String xaml = "Activity";
        private readonly String xamlx = "WorkflowService";
        // Namespaces
        private readonly String ns_xaml = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        private readonly String ns_xamlx = "http://schemas.microsoft.com/netfx/2009/xaml/servicemodel";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tokens">A Queue containing all tokens which shall be parsed</param>
        public Parser(Queue<IXamlElement> tokens)
        {
            this.tokens = tokens;
            this.initialTokenCount = tokens.Count;
        }

        /// <summary>
        /// Parses the Token-Queue and returns an equivalent IActivity representation
        /// </summary>
        /// <exception cref="System.NullReferenceException">Thrown when ModuleFactory or Token-Queue is null</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when Token-Queue is empty, but another token was expected</exception>
        /// <exception cref="WF2oWFN.API.ParseException">Thrown when an error occurs while parsing the Xaml tokens</exception>
        /// <returns>IActivity containing all parsed Activities</returns>
        public IActivity Parse()
        {
            // Activity AST
            IActivity activity = null;

            if (this.moduleFactory == null)
            {
                throw new NullReferenceException("No ModuleFactory was found");
            }
            if (this.tokens == null)
            {
                throw new NullReferenceException("No Token-Queue was found");
            }

            try
            {
                // [Start] Root Element
                IXamlElement startRoot = tokens.Dequeue();

                if (!RootTags.Contains(startRoot.QName))
                {
                    ParseException e = new ParseException(String.Format("No {0} or {1} was found", this.xaml, this.xamlx));
                    e.ElementNumber = initialTokenCount - tokens.Count;
                    e.Activity = "Root";
                    throw e;
                }
                else
                {
                    // Inner activity
                    activity = moduleFactory.CreateActivity(tokens.Peek().QName);

                    if (activity == null)
                    {
                        ParseException e = new ParseException(String.Format("No Module found for activity '{0}'", tokens.Peek().QName));
                        e.ElementNumber = initialTokenCount - tokens.Count;
                        throw e;
                    }
                    else
                    {
                        // Parse inner activity
                        activity = activity.Parse(tokens);
                    }
                }
                
                // [End] Root Element
                if (tokens.Count == 1)
                {
                    IXamlElement endRoot = tokens.Dequeue();

                    if (!RootTags.Contains(endRoot.QName))
                    {
                        throw new ParseException(String.Format("No {0} or {1} end tag was found", this.xaml, this.xamlx), initialTokenCount, "Root");
                    }
                }
                else
                {
                    throw new ParseException(String.Format("Expected end tag, but still too many tokens in queue '{0}'", tokens.Count), initialTokenCount - tokens.Count, "Root");
                }

                return activity;
            }
            catch (InvalidOperationException)
            {
                throw new ParseException("Expected token, but queue is empty", 1, "Root");
            }
        }

        #region Properties

        /// <summary>
        /// IModuleFactory Setter
        /// </summary>
        public IModuleFactory ModuleFactory
        {
            set { this.moduleFactory = value; }
        }

        #endregion Properties

        #region Private Methods

        private IList<String> RootTags
        {
            get
            {
                IList<String> tags = new List<String>();
                // Xaml
                tags.Add("{" + this.ns_xaml + "}" + this.xaml);
                // Xamlx
                tags.Add("{" + this.ns_xamlx + "}" + this.xamlx);

                return tags;
            }
        }

        #endregion Private Methods
    }
}
