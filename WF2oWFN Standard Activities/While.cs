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
    /// While Activity
    /// Consult Xaml reference in documentation for structural details.
    /// </summary>
    class While : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "While";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private IActivity innerActivity;
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
                if (token.Peek().QName.Equals(createQName("While.Condition")))
                {
                    // Start & End Element
                    token.Dequeue();
                    token.Dequeue();
                }
                // <Activity>
                IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                if (activity != null)
                {
                    innerActivity = activity.Parse(token);
                }
                else
                {
                    throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
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
            Transition init = phylum.NewTransition(prefix + "initactivity");
            Transition end = phylum.NewTransition(prefix + "endwhile");
            phylum.NewArc(p1, evalCondition);
            phylum.NewArc(evalCondition, condition);
            phylum.NewArc(condition, init);
            phylum.NewArc(condition, end);
            phylum.NewArc(end, p2);

            int currentID = 0;

            // Inner Activity
            if (innerActivity != null)
            {
                // New Activity
                phylum.ActivityCount += 1;
                currentID = phylum.ActivityCount;
                // Compile
                innerActivity.Compile(phylum);
                // Connect
                phylum.NewArc(init, currentID + ".internal.initialized");
                // Merge
                phylum.Merge(p1, currentID + ".internal.closed");
            }
            else
            {
                // Empty
                phylum.NewArc(init, p1);
            }

            return phylum;
        }

        #region Properties

        public IModuleFactory ModuleFactory
        {
            set { this.moduleFactory = value; }
        }

        public String QName
        {
            get { return "{" + this.ns + "}" + this.name; }
        }

        public String LocalName
        {
            get { return this.name; }
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
