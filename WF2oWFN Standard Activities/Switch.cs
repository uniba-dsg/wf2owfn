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
    /// Switch Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    class Switch : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Switch";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        private readonly String ns_x = "http://schemas.microsoft.com/winfx/2006/xaml";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private IList<IActivity> caseActivities = new List<IActivity>();
        private IActivity defaultActivity;
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
                if (token.Peek().QName.Equals(createQName("Switch.Default")))
                {
                    // Start Element
                    token.Dequeue();
                    // <Activity>
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                    if (activity != null)
                    {
                        defaultActivity = activity.Parse(token);
                    }
                    else
                    {
                        throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
                    // End Element
                    token.Dequeue();
                }
                else if (token.Peek().QName.Equals(createQName("Null", ns_x)))
                {
                    token.Dequeue();
                    token.Dequeue();
                    // Empty Case
                    caseActivities.Add(new Empty());
                }
                else
                {
                    // <Activity>
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                    if (activity != null)
                    {
                        caseActivities.Add(activity.Parse(token));
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
            Place lastState = p1;
            IList<int> mergeIds = new List<int>();

            for (int i = 1; i <= caseActivities.Count + 1; i++)
            {
                if (i <= caseActivities.Count)
                {
                    Place state = phylum.NewPlace(prefix + "case" + i);
                    Transition evalCase = phylum.NewTransition(prefix + "evalcase" + i);
                    Transition initCase = phylum.NewTransition(prefix + "initcase" + i);
                    phylum.NewArc(lastState, evalCase);
                    phylum.NewArc(evalCase, state);
                    phylum.NewArc(state, initCase);
                    // Activity
                    phylum.ActivityCount += 1;
                    int currentID = phylum.ActivityCount;
                    caseActivities[i - 1].Compile(phylum);
                    // Connect
                    phylum.NewArc(initCase, currentID + ".internal.initialized");
                    // LastState
                    lastState = state;
                    // Merge ID
                    mergeIds.Add(currentID);
                }
                else
                {
                    Transition initDefault = phylum.NewTransition(prefix + "initdefault");
                    phylum.NewArc(lastState, initDefault);

                    if (defaultActivity != null)
                    {
                        // Activity
                        phylum.ActivityCount += 1;
                        int currentID = phylum.ActivityCount;
                        defaultActivity.Compile(phylum);
                        // Connect
                        phylum.NewArc(initDefault, currentID + ".internal.initialized");
                        // Merge ID
                        mergeIds.Add(currentID);
                    }
                    else
                    {
                        // Empty Connect
                        phylum.NewArc(initDefault, p2);
                    }
                }
            }
            // Merge
            foreach(int id in mergeIds)
            {
                phylum.Merge(p2, id + ".internal.closed");
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

