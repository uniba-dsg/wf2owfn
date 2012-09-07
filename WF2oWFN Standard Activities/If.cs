//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WF2oWFN.API;
using WF2oWFN.API.Petri;

namespace WF2oWFN.Modules
{
    /// <summary>
    /// If Activity
    /// Consult Xaml reference in documentation for structural details.
    /// </summary>
    class If : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "If";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private IActivity innerThen;
        private IActivity innerElse;
        // TokenCount
        private int initialTokenCount;

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));
            // Debug
            initialTokenCount = token.Count;

            //Start Element
            token.Dequeue();

            // OPTIONAL Body
            while (!token.Peek().QName.Equals(this.QName) || !token.Peek().IsClosingElement)
            {
                // OPTIONAL <If.Then>
                if(token.Peek().QName.Equals(createQName("If.Then")))
                {
                    // Start Element
                    token.Dequeue();
                    // <Activity>
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                    if (activity != null)
                    {
                        innerThen = activity.Parse(token);
                    }
                    else
                    {
                        throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
                    // End Element
                    token.Dequeue();
                }
                // OPTIONAL <If.Else>
                else if (token.Peek().QName.Equals(createQName("If.Else")))
                {
                    // Start Element
                    token.Dequeue();
                    // <Activity>
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                    if (activity != null)
                    {
                        innerElse = activity.Parse(token);
                    }
                    else
                    {
                        // TODO Error no match
                    }
                    // End Element
                    token.Dequeue();
                }
                else
                {
                    throw new ParseException(String.Format("Unexpected token found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
                }
            }
            // End Element
            token.Dequeue();

            return this;
        }

        public PetriNet Compile(PetriNet phylum)
        {
            String prefix = phylum.ActivityCount + ".internal.";

            Place p1 = phylum.NewPlace(prefix + "initialized");
            Place p2 = phylum.NewPlace(prefix + "closed");

            // Inner Petri Net
            Place condition = phylum.NewPlace(prefix + "condition");
            Transition evalCondition = phylum.NewTransition(prefix + "evalcondition");
            phylum.NewArc(p1, evalCondition);
            phylum.NewArc(evalCondition, condition);

            // Empty
            if (innerThen == null && innerElse == null)
            {
                Transition finish = phylum.NewTransition(prefix + "finish");
                phylum.NewArc(condition, finish);
                phylum.NewArc(finish, p2);

                return phylum;
            }

            Transition initThen = phylum.NewTransition(prefix + "initthen");
            Transition initElse = phylum.NewTransition(prefix + "inittelse");
            phylum.NewArc(condition, initThen);
            phylum.NewArc(condition, initElse);

            // Then
            if (innerThen != null)
            {
                // New Activity
                phylum.ActivityCount += 1;
                int thenID = phylum.ActivityCount;
                // Compile
                innerThen.Compile(phylum);
                // Connect
                phylum.NewArc(initThen, thenID + ".internal.initialized");
                // Merge
                phylum.Merge(p2, thenID + ".internal.closed");
            }
            else
            {
                // Empty
                phylum.NewArc(initThen, p2);
            }

            // Else
            if (innerElse != null)
            {
                // New Activity
                phylum.ActivityCount += 1;
                int elseID = phylum.ActivityCount;
                // Compile
                innerElse.Compile(phylum);
                // Connect
                phylum.NewArc(initElse, elseID + ".internal.initialized");
                // Merge
                phylum.Merge(p2, elseID + ".internal.closed");
            }
            else
            {
                // Empty
                phylum.NewArc(initElse, p2);
            }

            return phylum;
        }

        #region Properties

        public String QName
        {
            get { return "{" + this.ns + "}" + this.name; }
        }

        public String LocalName
        {
            get { return this.name; }
        }

        public IModuleFactory ModuleFactory
        {
            set { this.moduleFactory = value; }
        }

        #endregion Properties

        #region Private Methods

        private String createQName(String localName)
        {
            return "{" + this.ns + "}" + localName;
        }

        private String createQName(String localName, String nameSpace)
        {
            return "{" + nameSpace + "}" + localName;
        }

        #endregion Private Methods
    }
}

