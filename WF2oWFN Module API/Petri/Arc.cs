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
    /// Petri Net Arc
    /// </summary>
    public class Arc
    {
        private Node source;
        private Node target;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The source node of the arc</param>
        /// <param name="target">The target node of the arc</param>
        public Arc(Node source, Node target)
        {
            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// The source node of the arc
        /// </summary>
        public Node Source
        {
            get { return source; }
            set { source = value; }
        }

        /// <summary>
        /// The target node of the arc
        /// </summary>
        public Node Target
        {
            get { return target; }
            set { target = value; }
        }
    }
}
