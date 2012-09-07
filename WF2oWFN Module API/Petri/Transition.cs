//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WF2oWFN.API.Petri
{
    /// <summary>
    /// Petri Net Transition
    /// </summary>
    public class Transition : Node
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Transition()
        {
            // Initialization
            preSet = new HashSet<Node>();
            postSet = new HashSet<Node>();
        }
    }
}
