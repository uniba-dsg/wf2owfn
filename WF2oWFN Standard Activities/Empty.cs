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
    /// Internal Empty Transition
    /// </summary>
    public class Empty : IPrimitive
    {
        private readonly String name = "Empty";
        private readonly String ns = "";

        public IActivity Parse(Queue<IXamlElement> token)
        {
            throw new NotImplementedException("No Xaml representation for an empty transition.");
        }

        public PetriNet Compile(PetriNet phylum)
        {
            String prefix = phylum.ActivityCount + ".internal.";
            // Places
            Place p1 = phylum.NewPlace(prefix + "initialized");
            Place p2 = phylum.NewPlace(prefix + "closed");
            // Transitions
            Transition empty = phylum.NewTransition(prefix + "empty");
            // Arcs
            phylum.NewArc(p1, empty);
            phylum.NewArc(empty, p2);

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
