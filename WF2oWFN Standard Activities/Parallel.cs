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
    /// Parallel Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    class Parallel : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Parallel";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private IList<IActivity> innerActivities = new List<IActivity>();
        // TokenCount
        private int initialTokenCount;

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));
            // Debug
            initialTokenCount = token.Count;

            //Start Element
            token.Dequeue();

            while (!token.Peek().QName.Equals(this.QName) || !token.Peek().IsClosingElement)
            {
                if (token.Peek().QName.Equals(createQName("Parallel.CompletionCondition")))
                {
                    token.Dequeue();
                    token.Dequeue();
                }
                else
                {
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                    if (activity != null)
                    {
                        IActivity inner = activity.Parse(token);
                        innerActivities.Add(inner);
                    }
                    else
                    {
                        throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
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
            if (innerActivities.Count == 1)
            {
                // New Activity
                phylum.ActivityCount += 1;
                int currentID = phylum.ActivityCount;
                // Compile
                innerActivities[0].Compile(phylum);
                // Connect
                phylum.Merge(p1, currentID + ".internal.initialized");
                phylum.Merge(currentID + ".internal.closed", p2);
            }
            else if (innerActivities.Count > 1)
            {
                Transition split = phylum.NewTransition(prefix + "split");
                Transition join = phylum.NewTransition(prefix + "join");
                phylum.NewArc(p1, split);
                phylum.NewArc(join, p2);

                for (int i = 1; i <= innerActivities.Count; i++)
                {
                    // New Activity
                    phylum.ActivityCount += 1;
                    int currentID = phylum.ActivityCount;
                    // Compile
                    innerActivities[i - 1].Compile(phylum);
                    // Connect
                    phylum.NewArc(split, currentID + ".internal.initialized");
                    phylum.NewArc(currentID + ".internal.closed", join);
                }
            }
            else
            {
                // Empty Sequence
                phylum.ActivityCount += 1;
                int currentID = phylum.ActivityCount;
                // Compile
                new Empty().Compile(phylum);
                // Connect
                phylum.Merge(p1, currentID + ".internal.initialized");
                phylum.Merge(p2, currentID + ".internal.closed");
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

        #endregion Private Methods
    }
}

