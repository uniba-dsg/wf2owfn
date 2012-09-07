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
    /// Petri Net Place
    /// </summary>
    public class Place : Node
    {
        private int tokens;
        private bool final;

        /// <summary>
        /// Constructor
        /// </summary>
        public Place()
        {
            // Initialization
            preSet = new HashSet<Node>();
            postSet = new HashSet<Node>();
            history = new Stack<String>();
        }

        /// <summary>
        /// Property for defining final places 
        /// </summary>
        public bool isFinal
        {
            get { return final; }
            set { final = value; }
        }

        /// <summary>
        /// Property for the token count
        /// </summary>
        public int Tokens
        {
            get { return tokens; }
            set { tokens = value; }
        }
    }
}
