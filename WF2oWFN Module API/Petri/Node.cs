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
    /// Petri Net Node
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// The name of the node
        /// </summary>
        protected String name;
        /// <summary>
        /// The preset of the node
        /// </summary>
        protected HashSet<Node> preSet;
        /// <summary>
        /// The postset of the node
        /// </summary>
        protected HashSet<Node> postSet;
        /// <summary>
        /// The communication type of the node (e.g. input)
        /// </summary>
        protected PetriNet.CommunicationType type;
        /// <summary>
        /// The history of the node (e.g. former names after merging nodes)
        /// </summary>
        protected Stack<String> history;

        /// <summary>
        /// The name of the Node
        /// </summary>
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The preset of the Node
        /// </summary>
        public HashSet<Node> PreSet
        {
            get { return preSet; }
            set { preSet = value; }
        }

        /// <summary>
        /// The postset of the Node
        /// </summary>
        public HashSet<Node> PostSet
        {
            get { return postSet; }
            set { postSet = value; }
        }

        /// <summary>
        /// The communication type of the node (e.g. input)
        /// </summary>
        public PetriNet.CommunicationType Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// The history of the node (e.g. former names after merging nodes)
        /// </summary>
        public Stack<String> History
        {
            get { return history; }
            set { history = value; }
        }
    }
}
