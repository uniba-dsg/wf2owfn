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
    /// Delay Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    public class Delay : IPrimitive
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Delay";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));

            // Start Element
            token.Dequeue();
            // End Element
            token.Dequeue();

            return this;
        }

        public PetriNet Compile(PetriNet phylum)
        {
            String prefix = phylum.ActivityCount + ".internal.";
            // Places
            Place p1 = phylum.NewPlace(prefix + "initialized");
            Place p2 = phylum.NewPlace(prefix + "closed");
            // Transitions
            Transition delay = phylum.NewTransition(prefix + "delay");
            // Arcs
            phylum.NewArc(p1, delay);
            phylum.NewArc(delay, p2);

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

        #endregion Properties
    }
}
