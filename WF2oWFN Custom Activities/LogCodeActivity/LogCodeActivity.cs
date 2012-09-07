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
    /// LogCodeActivity Custom Activity
    /// 
    /// Structure: <TERMINAL />
    /// </summary>
    public class LogCodeActivity : IPrimitive
    {
        private readonly String name = "LogCodeActivity";
        private readonly String ns = "clr-namespace:Custom_Activities;assembly=Custom Activities";

        public IActivity Parse(Queue<IXamlElement> token)
        {
            Console.WriteLine("Aufruf von Parse in {0}", name);

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
            Transition logCode = phylum.NewTransition(prefix + "logcode");
            // Arcs
            phylum.NewArc(p1, logCode);
            phylum.NewArc(logCode, p2);

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
