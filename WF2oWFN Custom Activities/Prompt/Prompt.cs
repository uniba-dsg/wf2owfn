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
    /// Prompt Custom Activity
    /// 
    /// Structure: <TERMINAL />
    /// </summary>
    public class Prompt : IPrimitive
    {
        private readonly String name = "Prompt";
        private readonly String ns = "clr-namespace:ActivityLibrary1;assembly=ActivityLibrary1";

        public IActivity Parse(Queue<IXamlElement> token)
        {
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
            Place message = phylum.NewPlace("in." + phylum.ActivityCount + ".prompt", PetriNet.CommunicationType.Input);
            // Transitions
            Transition receive = phylum.NewTransition(prefix + "receive");
            // Arcs
            phylum.NewArc(p1, receive);
            phylum.NewArc(receive, p2);
            phylum.NewArc(message, receive);

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
