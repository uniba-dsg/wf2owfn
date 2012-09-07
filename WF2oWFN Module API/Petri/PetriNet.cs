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
    /// A class representing a Petri Net
    /// </summary>
    public partial class PetriNet
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Identifier for activity scopes
        private int activityCount;
        // Places
        private HashSet<Place> internalPlaces;
        private HashSet<Place> inputPlaces;
        private HashSet<Place> outputPlaces;
        // Transitions
        private HashSet<Transition> transitions;
        // Ports
        private Dictionary<String, HashSet<Place>> ports;
        // Flow relation
        private HashSet<Arc> flow;
        // Final sets
        private List<HashSet<Place>> finalSetList;

        /// <summary>
        /// Enumeration for the communication types of nodes
        /// </summary>
        public enum CommunicationType
        {
            /// <summary>
            /// Internal nodes
            /// </summary>
            Internal,
            /// <summary>
            /// Input nodes
            /// </summary>
            Input,
            /// <summary>
            /// Output nodes
            /// </summary>
            Output,
            /// <summary>
            /// In- and Output nodes
            /// </summary>
            InOut
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public PetriNet()
        {
            // Initialization
            internalPlaces = new HashSet<Place>();
            inputPlaces = new HashSet<Place>();
            outputPlaces = new HashSet<Place>();
            transitions = new HashSet<Transition>();
            flow = new HashSet<Arc>();
            ports = new Dictionary<String, HashSet<Place>>();
            finalSetList = new List<HashSet<Place>>();
            activityCount = 0;
        }

        /// <summary>
        /// Adds a new internal place to the petri net
        /// </summary>
        /// <param name="id">Unique identifier for the place (used in history)</param>
        /// <exception cref="System.ArgumentException">If the place with the given <c>id</c> already exists</exception>
        /// <returns>The place</returns>
        public Place NewPlace(String id)
        {
            return NewPlace(id, CommunicationType.Internal);
        }

        /// <summary>
        /// Adds a new place to the petri net
        /// </summary>
        /// <param name="id">Unique identifier for the place (used in history)</param>
        /// <param name="type">The communication type of the place (e.g. input)</param>
        /// <exception cref="System.ArgumentException">If the place with the given <c>id</c> already exists or is empty</exception>
        /// <returns>The place</returns>
        public Place NewPlace(String id, CommunicationType type)
        {
            if (id == String.Empty)
            {
                throw new ArgumentException("Specified id cannot be empty");
            }

            Place place = new Place();
            place.Name = id;
            // Unique?
            Place p = Find(id);

            if (p != null)
            {
                throw new ArgumentException("Place with given id '{0}' already exists");
            }
            else
            {
                // Place Type
                place.Type = type;
                switch (type)
                {
                    case CommunicationType.Input:
                        inputPlaces.Add(place);
                        break;
                    case CommunicationType.Output:
                        outputPlaces.Add(place);
                        break;
                    default:
                        internalPlaces.Add(place);
                        break;
                }
                // History
                place.History.Push(id);

                return place;
            }
        }

        /// <summary>
        /// Adds a new transition to the petri net
        /// </summary>
        /// <param name="id">Unique identifier for the transition</param>
        /// <exception cref="System.ArgumentException">If the transition with the given <c>id</c> already exists or is empty</exception>
        /// <returns>The transition</returns>
        public Transition NewTransition(String id)
        {
            if (id == String.Empty)
            {
                throw new ArgumentException("Specified id cannot be empty");
            }

            // No History for transitions yet!
            Transition transition = new Transition();
            transition.Name = id;

            // Unique?
            try
            {
                Transition e = transitions.First(t => t.Name.Contains(id));
            }
            catch (InvalidOperationException)
            {
                transitions.Add(transition);
            }

            return transition;
        }

        /// <summary>
        /// Adds a new arc to the petri net
        /// </summary>
        /// <param name="source">The source node of the arc</param>
        /// <param name="target">The target node of the arc</param>
        /// <exception cref="System.ArgumentException">If the source-target nodes are not place-transition or transition-place combinations</exception>
        /// <exception cref="System.NullReferenceException">If the <c>source</c> or <c>target</c> node are <c>null</c></exception>
        /// <returns>The arc</returns>
        public Arc NewArc(Node source, Node target)
        {
            if (source == null)
            {
                throw new NullReferenceException("Specified source cannot be null.");
            }
            if (target == null)
            {
                throw new NullReferenceException("Specified target cannot be null.");
            }

            // Get recent references, they may have changed due to merging, (only places can change yet)
            if (source is Place)
            {
                source = Find(source.History.Peek());
            }
            if (target is Place)
            {
                target = Find(target.History.Peek());
            }

            // Only place (transition) to transition (place)
            if (source is Place)
            {
                if (!(target is Transition))
                {
                    throw new ArgumentException("Arcs from place to place are not allowed");
                }
            }
            else
            {
                if (!(target is Place))
                {
                    throw new ArgumentException("Arcs from transition to transition are not allowed");
                }
            }

            // New Arc
            Arc arc = new Arc(source, target);
            flow.Add(arc);

            // Transition Pre-/Post-Set
            source.PostSet.Add(target);
            target.PreSet.Add(source);
            
            return arc;
        }

        /// <summary>
        /// Adds a new arc to the petri net
        /// </summary>
        /// <param name="source">The source node of the arc</param>
        /// <param name="id2">Unique identifier for the target</param>
        /// <exception cref="System.ArgumentException">If the source-target nodes are not place-transition or transition-place combinations</exception>
        /// <exception cref="System.NullReferenceException">If the <c>source</c> or <c>target</c> node are <c>null</c></exception>
        /// <returns>The arc</returns>
        public Arc NewArc(Node source, String id2)
        {
            Place target = Find(id2);
            
            // If target is null error pops up in newArc()
            return NewArc(source, target);
        }

        /// <summary>
        /// Adds a new arc to the petri net
        /// </summary>
        /// <param name="id1">Unique identifier for the source</param>
        /// <param name="target">The target node of the arc</param>
        /// <exception cref="System.ArgumentException">If the source-target nodes are not place-transition or transition-place combinations</exception>
        /// <exception cref="System.NullReferenceException">If the <c>source</c> or <c>target</c> node are <c>null</c></exception>
        /// <returns>The arc</returns>
        public Arc NewArc(String id1, Node target)
        {
            Place source = Find(id1);

            // If source is null error pops up in newArc()
            return NewArc(source, target);
        }

        /// <summary>
        /// Merges two places in the petri net
        /// </summary>
        /// <param name="p1">Place to merge</param>
        /// <param name="p2">Place to merge</param>
        /// <remarks>Currently the new place uses the name of <c>p1</c>, both names are kept in history tho</remarks>
        /// <exception cref="System.NullReferenceException">If any of the places is <c>null</c></exception>
        public void Merge(Place p1, Place p2)
        {
            // Get recent references
            p1 = Find(p1.History.Peek());
            p2 = Find(p2.History.Peek());

            if (p1 == p2 && p1 != null)
            {
                return;
            }
            if (p1 == null || p2 == null)
            {
                throw new NullReferenceException("Places cannot be null.");
            }

            // New Place
            Place p12 = new Place();
            // Use name of first argument
            p12.Name = p1.Name;
            p12.Tokens = Math.Max(p1.Tokens, p2.Tokens);
            p12.isFinal = (p1.isFinal || p2.isFinal);

            // History union
            p12.History = new Stack<String>(p12.History.Union(p1.History.Union(p2.History)));

            // New Preset/Postset
            HashSet<Node> presetDistinct = new HashSet<Node>(p1.PreSet.Union(p2.PreSet));
            HashSet<Node> postsetDistinct = new HashSet<Node>(p1.PostSet.Union(p2.PostSet));
            p12.PreSet = presetDistinct;
            p12.PostSet = postsetDistinct;

            // Final sets
            foreach (HashSet<Place> finalSet in finalSetList)
            {
                // Iterate from last element to allow delete operation
                for (int i = finalSet.Count - 1; i >= 0; i--)
                {
                    if (finalSet.ElementAt(i).History.Contains(p1.Name) || finalSet.ElementAt(i).History.Contains(p2.Name))
                    {
                        // Remove
                        finalSet.Remove(finalSet.ElementAt(i));
                        // Add new place
                        finalSet.Add(p12);
                    }
                }
            }

            // Remove Places
            Remove(p1);
            Remove(p2);

            // Add merged Place
            newPlace(p12);

            // New Arcs
            foreach (Node node in presetDistinct)
            {
                NewArc(node, p12);
            }

            foreach (Node node in postsetDistinct)
            {
                NewArc(p12, node);
            }
        }

        /// <summary>
        /// Merges two places in the petri net
        /// </summary>
        /// <param name="p1">Place to merge</param>
        /// <param name="id2">Unique identifier of the place to merge</param>
        /// <remarks>Currently the new place uses the name of <c>p1</c>, both names are kept in history tho</remarks>
        /// <exception cref="System.NullReferenceException">If any of the places is <c>null</c></exception>
        public void Merge(Place p1, String id2)
        {
            Place p2 = Find(id2);

            // If p2 is null error pops up in Merge()
            Merge(p1, p2);
        }

        /// <summary>
        /// Merges two places in the petri net
        /// </summary>
        /// <param name="id1">Unique identifier of the place to merge</param>
        /// <param name="p2">Place to merge</param>
        /// <remarks>Currently the new place uses the name of <c>p1</c>, both names are kept in history tho</remarks>
        /// <exception cref="System.NullReferenceException">If any of the places is <c>null</c></exception>
        public void Merge(String id1, Place p2)
        {
            Place p1 = Find(id1);

            // If p1 is null error pops up in Merge()
            Merge(p1, p2);
        }

        /// <summary>
        /// Merges two places in the petri net
        /// </summary>
        /// <param name="id1">Unique identifier of the place to merge</param>
        /// <param name="id2">Unique identifier of the place to merge</param>
        /// <remarks>Currently the new place uses the name of <c>p1</c>, both names are kept in history tho</remarks>
        /// <exception cref="System.NullReferenceException">If any of the places is <c>null</c></exception>
        public void Merge(String id1, String id2)
        {
            Place p1 = Find(id1);
            Place p2 = Find(id2);

            // If p1 or p2 is null error pops up in Merge()
            Merge(p1, p2);
        }

        /// <summary>
        /// Removes a place from the petri net
        /// </summary>
        /// <param name="place">The place to remove</param>
        public void Remove(Place place)
        {
            if (place == null)
            {
                return;
            }
            // Remove Arcs
            try
            {
                flow.RemoveWhere(arc => arc.Source.Equals(place) || arc.Target.Equals(place));
            }
            catch (ArgumentNullException)
            {
                // No arcs
            }
            // Update Pre/Postsets
            foreach (Node node in place.PreSet)
            {
                node.PostSet.Remove(place);
            }
            foreach (Node node in place.PostSet)
            {
                node.PreSet.Remove(place);
            }
            // Remove Place
            switch (place.Type)
            {
                case (CommunicationType.Input):
                    inputPlaces.Remove(place); 
                    break;
                case (CommunicationType.Output): 
                    outputPlaces.Remove(place);
                    break;
                default: 
                    internalPlaces.Remove(place);
                    break;
            }
        }

        /// <summary>
        /// Finds a place in the petri net
        /// </summary>
        /// <param name="id">Unique identifier of the place to find</param>
        /// <returns>The place or <c>null</c> if no place was found</returns>
        public Place Find(String id)
        {
            try
            {
                Place intPlace = internalPlaces.First(e => e.History.Contains(id));
                return intPlace;
            }
            catch (InvalidOperationException)
            {
                try
                {
                    Place inPlace = inputPlaces.First(e => e.History.Contains(id));
                    return inPlace;
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        Place outPlace = outputPlaces.First(e => e.History.Contains(id));
                        return outPlace;
                    }
                    catch (InvalidOperationException)
                    {
                        // No Place found
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new final set to the final set list
        /// </summary>
        /// <param name="finalSet"></param>
        /// <exception cref="System.NullReferenceException">If the <c>finalSet</c> is <c>null</c></exception>
        public void AddFinalSet(HashSet<Place> finalSet)
        {
            if (finalSet == null)
            {
                new NullReferenceException("Final set cannot be null");
            }
            finalSetList.Add(finalSet);
        }

        /// <summary>
        /// Property for the activity scope identifier
        /// </summary>
        public int ActivityCount
        {
            get { return activityCount; }
            set { activityCount = value; }
        }

        /// <summary>
        /// Aggregates statistical data about the petri net.
        /// </summary>
        /// <returns>A statistical summary of the petri net</returns>
        public String GetStatistics()
        {
            String statistics;

            statistics = "|P|= " + (internalPlaces.Count + inputPlaces.Count + outputPlaces.Count);
            statistics += ", |P_in|= " + inputPlaces.Count;
            statistics += ", |P_out|= " + outputPlaces.Count;
            statistics += ", |T|= " + transitions.Count;
            statistics += ", |F|= " + flow.Count;

            return statistics;
        }

        #region Private Methods

        /// <summary>
        /// Adds a new internal place to the petri net
        /// </summary>
        /// <param name="place">The place to add</param>
        /// <exception cref="System.ArgumentException">If the given <c>place</c> already exists</exception>
        /// <returns>The place</returns>
        private Place newPlace(Place place)
        {
            if (Find(place.History.Peek()) == null)
            {
                // Place Type
                switch (place.Type)
                {
                    case CommunicationType.Input:
                        inputPlaces.Add(place);
                        break;
                    case CommunicationType.Output:
                        outputPlaces.Add(place);
                        break;
                    default:
                        internalPlaces.Add(place);
                        break;
                }
                return place;
            }
            else
            {
                throw new ArgumentException("Place with given id '{0}' already exists", place.History.Peek());
            }
        }

        #endregion Private Methods

        #region Static Methods

        /// <summary>
        /// Returns the weight of the arc between the <c>source</c> and <c>target</c> node
        /// <remarks>Currently fixed to a cardinality of one and only used by petri het output</remarks>
        /// </summary>
        /// <param name="source">The source node</param>
        /// <param name="target">The target node</param>
        /// <returns>The weight of the node or null if no arc exists between the nodes</returns>
        public int GetArcWeight(Node source, Node target)
        {
            // For now fixed to one, dummy output
            return 1;
        }

        #endregion Static Methods
    }
}