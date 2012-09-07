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
    /// Pick Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    class Pick : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Pick";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private IList<Branch> branches = new List<Branch>();
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
                while (token.Peek().QName.Equals(createQName("PickBranch")))
                {
                    // AST
                    Branch branch = new Branch();
                    // Start Element
                    token.Dequeue();
                    // <PickBranch.Trigger>
                    if (token.Peek().QName.Equals(createQName("PickBranch.Trigger")))
                    {
                        // Start Element
                        token.Dequeue();
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                        if (activity != null)
                        {
                            branch.Trigger = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }
                        // End Element
                        token.Dequeue();
                    }
                    if(!token.Peek().QName.Equals(createQName("PickBranch")))
                    {
                        // [ <Activity> ]
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                        if (activity != null)
                        {
                            branch.Action = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }
                    }
                    // AST
                    branches.Add(branch);
                    // End Element
                    token.Dequeue();
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
            if (branches.Count != 0)
            {
                for (int i = 1; i <= branches.Count; i++)
                {
                    // Trigger
                    phylum.ActivityCount += 1;
                    int triggerID = phylum.ActivityCount;
                    // Compile
                    branches[i - 1].Trigger.Compile(phylum); 
                    // Connect
                    phylum.Merge(p1, triggerID + ".internal.initialized");

                    if (branches[i - 1].Action != null)
                    {
                        // New Activity
                        phylum.ActivityCount += 1;
                        int currentID = phylum.ActivityCount;
                        // Compile
                        branches[i - 1].Action.Compile(phylum);
                        // Connect
                        phylum.Merge(triggerID + ".internal.closed", currentID + ".internal.initialized");
                        phylum.Merge(p2, currentID + ".internal.closed");
                    }
                    else
                    {
                        // Empty
                        phylum.Merge(triggerID + ".internal.closed", p2);
                    }
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

        class Branch
        {
            IActivity trigger;
            IActivity action;

            public IActivity Trigger
            {
                get { return trigger; }
                set { trigger = value; }
            }

            public IActivity Action
            {
                get { return action; }
                set { action = value; }
            }
        }

        #endregion Private Methods
    }
}
