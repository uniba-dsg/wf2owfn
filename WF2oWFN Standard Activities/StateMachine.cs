//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WF2oWFN.API;
using WF2oWFN.API.Petri;

namespace WF2oWFN.Modules
{
    /// <summary>
    /// StateMachine Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    public class StateMachine : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "StateMachine";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        private readonly String ns_x = "http://schemas.microsoft.com/winfx/2006/xaml";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private String initialState;
        private int triggerKey;
        private IList<SMState> states = new List<SMState>();
        private IDictionary<String, SMTrigger> triggers = new Dictionary<String, SMTrigger>();
        // TokenCount
        private int initialTokenCount;

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));
            // Debug
            initialTokenCount = token.Count;

            // [Start] StateMachine
            IXamlElement root = token.Dequeue();

            // Initial State
            if (root.Attributes.TryGetValue("InitialState", out initialState))
            {
                initialState = extractReference(initialState);

                while (!token.Peek().QName.Equals(this.QName))
                {
                    // Parse State
                    parseState(token);
                }
            }
            else
            {
                while (!token.Peek().QName.Equals(this.QName))
                {
                    if (token.Peek().QName.Equals(createQName("StateMachine.InitialState", ns)))
                    {
                        // [Start] InitialState
                        token.Dequeue();
                        String reference = token.Peek().Attributes["x:Name"];
                        initialState = reference;

                        while (!token.Peek().QName.Equals(createQName("StateMachine.InitialState", ns)))
                        {
                            // Parse State
                            parseState(token);
                        }
                        // [End] InitialState
                        token.Dequeue();
                    }
                    else
                    {
                        // Parse State
                        parseState(token);
                    }
                }
            }
            
            // [End] StateMachine
            token.Dequeue();

            return this;
        }

        public PetriNet Compile(PetriNet phylum)
        {
            String prefix = phylum.ActivityCount + ".internal.";

            Place p1 = phylum.NewPlace(prefix + "initialized");
            Place p2 = phylum.NewPlace(prefix + "closed");
            // Create States
            foreach (SMState state in states)
            {
                String prefixState = prefix + state.Id + ".";
                IList<Place> actions = new List<Place>();
                // Entry Place closed
                int entryClosed;

                Place stp1 = phylum.NewPlace(prefixState + "initialized");

                if (state.Final)
                {
                    // Closed
                    Place stp2 = phylum.NewPlace(prefixState + "closed");
                    // Activity
                    if (state.Entry is Empty)
                    {
                        // Empty
                        phylum.ActivityCount += 1;
                        entryClosed = phylum.ActivityCount;
                        Place entryP2 = phylum.NewPlace(prefix + "internal.closed");
                        phylum.Merge(stp1, entryP2);
                        phylum.Merge(stp1, stp2);
                    }
                    else
                    {
                        // Entry
                        Place entryP1 = phylum.NewPlace(prefixState + "entry.initialized");
                        Place entryP2 = phylum.NewPlace(prefixState + "entry.closed");
                        phylum.ActivityCount += 1;
                        entryClosed = phylum.ActivityCount;
                        state.Entry.Compile(phylum);
                        // Merge
                        phylum.Merge(entryP1, entryClosed + ".internal.initialized");
                        phylum.Merge(entryP2, entryClosed + ".internal.closed");
                        phylum.Merge(stp1, entryP1);
                        phylum.Merge(entryP2, stp2);
                    }
                }
                else
                {
                    // Entry
                    if(state.Entry is Empty)
                    {
                        phylum.ActivityCount += 1;
                        entryClosed = phylum.ActivityCount;
                        Place entryP2 = phylum.NewPlace(entryClosed + ".internal.closed");
                        phylum.Merge(stp1, entryP2);
                    }
                    else
                    {
                        // Entry
                        Place entryP1 = phylum.NewPlace(prefixState + "entry.initialized");
                        Place entryP2 = phylum.NewPlace(prefixState + "entry.closed");
                        phylum.ActivityCount += 1;
                        entryClosed = phylum.ActivityCount;
                        state.Entry.Compile(phylum);
                        // Merge
                        phylum.Merge(entryP1, entryClosed + ".internal.initialized");
                        phylum.Merge(entryP2, entryClosed + ".internal.closed");
                        phylum.Merge(stp1, entryP1);
                    }

                    // Exit
                    Place exitP1;
                    Place exitP2;

                    if (state.Exit is Empty)
                    {
                        exitP1 = phylum.NewPlace(prefixState + "exit");
                        exitP2 = exitP1;
                    }
                    else
                    {
                        exitP1 = phylum.NewPlace(prefixState + "exit.initialized");
                        exitP2 = phylum.NewPlace(prefixState + "exit.closed");
                        // Activity
                        phylum.ActivityCount += 1;
                        int currentID = phylum.ActivityCount;
                        state.Exit.Compile(phylum);
                        // Merge
                        phylum.Merge(exitP1, currentID + ".internal.initialized");
                        phylum.Merge(exitP2, currentID + ".internal.closed");
                    }

                    // Transitions
                    for (int i = 1; i <= state.Transitions.Count; i++)
                    {
                        SMTransition transition = state.Transitions[i - 1];
                        // To Output Places
                        Transition tsplit = phylum.NewTransition(prefixState + "t" + i);
                        Place wait = phylum.NewPlace(prefixState + "t" + i + "wait");
                        Place to = phylum.NewPlace(prefix + state.Id + ".to" + i + "." + transition.To);
                        // Actions
                        Place actionP1;
                        Place actionP2;

                        if (transition.Action is Empty)
                        {
                            actionP1 = phylum.NewPlace(prefixState + transition.To + i + ".action.initialized");
                            actionP2 = actionP1;
                        }
                        else
                        {
                            actionP1 = phylum.NewPlace(prefixState + transition.To + i + ".action.initialized");
                            actionP2 = phylum.NewPlace(prefixState + transition.To + i + ".action.closed");
                            actions.Add(actionP1);
                            // Activity
                            phylum.ActivityCount += 1;
                            int currentID = phylum.ActivityCount;
                            transition.Action.Compile(phylum);
                            // Merge
                            phylum.Merge(actionP1, currentID + ".internal.initialized");
                            phylum.Merge(actionP2, currentID + ".internal.closed");
                        }
                        // Connect
                        Transition split = phylum.NewTransition(prefixState + transition.To + ".split" + i);
                        phylum.NewArc(actionP2, split);
                        phylum.NewArc(split, exitP1);
                        phylum.NewArc(split, wait);
                        phylum.NewArc(wait, tsplit);
                        // Exit Split
                        phylum.NewArc(exitP2, tsplit);
                        phylum.NewArc(tsplit, to);
                    }

                    // Create Trigger
                    foreach (String key in triggers.Keys)
                    {
                        SMTrigger trigger = triggers[key];
                        if (trigger.Transitions.Any(t => state.Transitions.Contains(t)))
                        {
                            // Trigger
                            Place triggerIn = phylum.NewPlace(prefixState + "trigger" + key + ".in");
                            Place triggerOut = phylum.NewPlace(prefixState + "trigger" + key + ".out");
                            // Activity
                            phylum.ActivityCount += 1;
                            int currentID = phylum.ActivityCount;
                            trigger.Trigger.Compile(phylum);
                            // Merge
                            phylum.Merge(triggerIn, currentID + ".internal.initialized");
                            phylum.Merge(triggerOut, currentID + ".internal.closed");

                            // Connect
                            phylum.Merge(entryClosed + ".internal.closed", triggerIn);
                            Transition on = phylum.NewTransition(prefixState + "ontrigger" + key);
                            phylum.NewArc(triggerOut, on);
                            Place runCondition = phylum.NewPlace(prefixState + "runcondition1" + key);
                            phylum.NewArc(on, runCondition);
                            // Conditions
                            Place lastCondition = runCondition;
                            for (int i = 1; i <= trigger.Transitions.Count; i++)
                            {
                                // Null Trigger
                                if (!trigger.Transitions[i - 1].Condition)
                                {
                                    Transition start = phylum.NewTransition(prefixState + "astart" + i + key);
                                    phylum.NewArc(lastCondition, start);
                                    // Connect Action
                                    int id = state.Transitions.IndexOf(trigger.Transitions[i - 1]) + 1;
                                    phylum.NewArc(start, prefixState + trigger.Transitions[i - 1].To + id + ".action.initialized");

                                    // Flag Transition
                                    state.Transitions.First(t => t.Equals(trigger.Transitions[i - 1])).Flag = true;
                                }
                                else
                                {
                                    Transition eval = phylum.NewTransition(prefixState + "eval" + i + key);
                                    phylum.NewArc(lastCondition, eval);
                                    Place condition = phylum.NewPlace(prefixState + "condition" + i + key);
                                    phylum.NewArc(eval, condition);
                                    // False
                                    Transition wrong = phylum.NewTransition(prefixState + "wrong" + i + key);
                                    phylum.NewArc(condition, wrong);

                                    // Reset
                                    if (i == trigger.Transitions.Count)
                                    {
                                        phylum.NewArc(wrong, triggerIn);
                                    }
                                    else
                                    {
                                        Place newRun = phylum.NewPlace(prefixState + "runcondition" + (i + 1) + key);
                                        phylum.NewArc(wrong, newRun);
                                        lastCondition = newRun;
                                    }

                                    // True
                                    Transition right = phylum.NewTransition(prefixState + "astart" + i + key);
                                    phylum.NewArc(condition, right);
                                    // Connect Action
                                    int id = state.Transitions.IndexOf(trigger.Transitions[i - 1]) + 1;
                                    phylum.NewArc(right, prefixState + trigger.Transitions[i - 1].To + id + ".action.initialized");

                                    // Flag Transition
                                    state.Transitions.First(t => t.Equals(trigger.Transitions[i - 1])).Flag = true;
                                }
                            }
                        }
                    }
                    // Empty Trigger
                    int emptyTrigger = countEmptyTrigger(state.Transitions);

                    if (emptyTrigger > 0)
                    {
                        // Connect
                        Transition on = phylum.NewTransition(prefixState + "onemptytrigger");
                        Place trigger = phylum.NewPlace(prefixState + "trigger");
                        phylum.Merge(entryClosed + ".internal.closed", trigger);
                        phylum.NewArc(trigger, on);
                        Place runCondition = phylum.NewPlace(prefixState + "runcondition1");
                        phylum.NewArc(on, runCondition);
                        // Conditions
                        Place lastCondition = runCondition;
                        for (int i = 1; i <= state.Transitions.Count; i++)
                        {
                            if (!state.Transitions[i - 1].Flag)
                            {
                                // Null Trigger
                                if (!state.Transitions[i - 1].Condition)
                                {
                                    int id = state.Transitions.IndexOf(state.Transitions[i - 1]) + 1;
                                    Transition start = phylum.NewTransition(prefixState + "bstart" + i + id);
                                    phylum.NewArc(lastCondition, start);
                                    // Connect Action
                                    phylum.NewArc(start, prefixState + state.Transitions[i - 1].To + id + ".action.initialized");
                                }
                                else
                                {
                                    Transition eval = phylum.NewTransition(prefixState + "eval" + i);
                                    phylum.NewArc(lastCondition, eval);
                                    Place condition = phylum.NewPlace(prefixState + "condition" + i);
                                    phylum.NewArc(eval, condition);
                                    // False
                                    Transition wrong = phylum.NewTransition(prefixState + "wrong" + i);
                                    phylum.NewArc(condition, wrong);

                                    // Reset
                                    if (i == emptyTrigger)
                                    {
                                        phylum.NewArc(wrong, trigger);
                                    }
                                    else
                                    {
                                        Place newRun = phylum.NewPlace(prefixState + "runcondition" + (i + 1));
                                        phylum.NewArc(wrong, newRun);
                                        lastCondition = newRun;
                                    }

                                    // True
                                    int id = i;
                                    Transition right = phylum.NewTransition(prefixState + "bstart" + i + id);
                                    phylum.NewArc(condition, right);
                                    // Connect Action
                                    phylum.NewArc(right, prefixState + state.Transitions[i - 1].To + id + ".action.initialized");
                                }
                            }
                        }
                    }
                }
            }

            // Connect States
            // Initial State
            phylum.Merge(p1, prefix + states.First(s => s.Id.Equals(initialState)).Id + ".initialized");
            // Final States
            IList<SMState> finalStates = new List<SMState>(states.Where(s => s.Final == true));
            foreach (SMState finalState in finalStates)
            {
                phylum.Merge(p2, prefix + finalState.Id + ".closed");
            }
            // Transitions
            foreach (SMState state in states)
            {
                for (int i = 1; i <= state.Transitions.Count; i++)
                {
                    SMTransition transition = state.Transitions[i - 1];
                    phylum.Merge(prefix + state.Id + ".to" + i + "." +transition.To, prefix + transition.To + ".initialized");
                }
            }

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

        public IModuleFactory ModuleFactory
        {
            set { this.moduleFactory = value; }
        }

        #endregion Properties

        #region Private Methods

        private void parseState(Queue<IXamlElement> token)
        {
            if (token.Peek().QName.Equals(createQName("State")))
            {
                // State
                SMState state = new SMState();
                states.Add(state);

                // [Start] State
                IXamlElement element = token.Dequeue();

                // Reference
                String reference = element.Attributes["x:Name"];
                state.Id = reference;
                // Final State
                String att;
                if(element.Attributes.TryGetValue("IsFinal", out att))
                {
                    if (att.Equals("True")) { state.Final = true; }
                }

                while (!token.Peek().QName.Equals(createQName("State")))
                {
                    if (token.Peek().QName.Equals(createQName("State.Entry")))
                    {
                        // [Start] State.Entry
                        token.Dequeue();
                        // Activity
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);
                        if (activity != null)
                        {
                            state.Entry = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }
                        // [End] State.Entry
                        token.Dequeue();
                    }
                    else if (token.Peek().QName.Equals(createQName("State.Exit")))
                    {
                        // [Start] State.Exit
                        token.Dequeue();
                        // Activity
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);
                        if (activity != null)
                        {
                            state.Exit = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }
                        // [End] State.Exit
                        token.Dequeue();
                    }
                    else if (token.Peek().QName.Equals(createQName("State.Transitions")))
                    {
                        // [Start] State.Transitions
                        token.Dequeue();

                        // Transitions
                        while (!token.Peek().QName.Equals(createQName("State.Transitions")))
                        {
                            // Transition
                            state.Transitions.Add(parseTransition(token));
                        }
                        // [End] State.Transitions
                        token.Dequeue();
                    }
                    else
                    {
                        throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
                }

                // [End] State
                token.Dequeue();
            }
            else if (token.Peek().QName.Equals(createQName("Reference", ns_x)))
            {
                // [Start][End] Reference
                token.Dequeue();
                token.Dequeue();
            }
            else
            {
                throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
            }
        }

        private SMTransition parseTransition(Queue<IXamlElement> token)
        {
            // Transition
            SMTransition transition = new SMTransition();
            // Trigger
            SMTrigger trigger;
            // Reference
            String reference;
            String refTrigger;
            String condition;
            // [Start] Transition
            IXamlElement element = token.Dequeue();
            bool attRef = element.Attributes.TryGetValue("To", out reference);
            bool attTrigger = element.Attributes.TryGetValue("Trigger", out refTrigger);
            bool attCondition = element.Attributes.TryGetValue("Condition", out condition);
            // To
            if (attRef)
            {
                transition.To = extractReference(reference);
            }
            // Trigger
            if (attTrigger)
            {
                String id = extractReference(refTrigger);

                if (triggers.ContainsKey(id))
                {
                    triggers[id].Transitions.Add(transition);
                }
                else
                {
                    // Warning
                    log.Warn("Warning Trigger created before IActivity was found!");
                    trigger = new SMTrigger();
                    trigger.Transitions.Add(transition);
                    triggers.Add(id, trigger);
                }
            }
            // Condition
            if (attCondition)
            {
                transition.Condition = true;
            }

            while (!token.Peek().QName.Equals(createQName("Transition")))
            {
                if (token.Peek().QName.Equals(createQName("Transition.Trigger")))
                {
                    // [Start] Transition.Trigger
                    token.Dequeue();
                    // Trigger Reference
                    String triggerRef;
                    if (token.Peek().Attributes.TryGetValue("x:Name", out triggerRef))
                    {
                        // Activity
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);
                        IActivity triggerActivity;

                        if (activity != null)
                        {
                            triggerActivity = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }

                        if (triggers.ContainsKey(triggerRef))
                        {
                            log.Warn("Warning trigger existent!");
                            triggers[triggerRef].Trigger = triggerActivity;
                            triggers[triggerRef].Transitions.Add(transition);
                        }
                        else
                        {
                            trigger = new SMTrigger();
                            trigger.Trigger = triggerActivity;
                            trigger.Transitions.Add(transition);
                            triggers.Add(triggerRef, trigger);
                        }
                    }
                    else
                    {
                        // Activity
                        IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);
                        IActivity triggerActivity;

                        if (activity != null)
                        {
                            triggerActivity = activity.Parse(token);
                        }
                        else
                        {
                            throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                        }
                        trigger = new SMTrigger();
                        trigger.Trigger = triggerActivity;
                        trigger.Transitions.Add(transition);
                        triggerKey++;
                        triggers.Add(triggerKey.ToString(), trigger);
                    }

                    // [End] Transition.Trigger
                    token.Dequeue();
                }
                else if (token.Peek().QName.Equals(createQName("Transition.Condition")))
                {
                    // [Start] Transition.Condition
                    token.Dequeue();
                    transition.Condition = true;
                    // [End] Transition.Condition
                    token.Dequeue();
                }
                else if (token.Peek().QName.Equals(createQName("Transition.Action")))
                {
                    // [Start] Transition.Action
                    token.Dequeue();
                    // Activity
                    IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);
                    if (activity != null)
                    {
                        transition.Action = activity.Parse(token);
                    }
                    else
                    {
                        throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
                    // [End] Transition.Action
                    token.Dequeue();
                }
                else if (token.Peek().QName.Equals(createQName("Transition.To")))
                {
                    // [Start] Transition.To
                    token.Dequeue();

                    if (token.Peek().QName.Equals(createQName("State")))
                    {
                        transition.To = token.Peek().Attributes["x:Name"];
                        // Recursion
                        parseState(token);
                    }
                    else if (token.Peek().QName.Equals(createQName("Reference", ns_x)))
                    {
                        // [Start] Reference
                        element = token.Dequeue();
                        reference = element.Attributes["Reference"];
                        transition.To = reference;
                        // [End] Reference
                        token.Dequeue();
                    }
                    else
                    {
                        throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
                    }
                    // [End] Transition.To
                    token.Dequeue();
                }
                else
                {
                    throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
                }
            }
            // [End] Transition
            token.Dequeue();

            return transition;
        }

        private int countEmptyTrigger(IList<SMTransition> iList)
        {
            int count = 0;

            foreach (SMTransition t in iList)
            {
                if (!t.Flag)
                {
                    count++;
                }
            }

            return count;
        }

        private String extractReference(String attribute)
        {
            String[] arr = attribute.Substring(1, attribute.Length - 2).Split(' ');
            return arr[1];
        }

        private String createQName(String localName)
        {
            return "{" + this.ns + "}" + localName;
        }

        private String createQName(String localName, String nameSpace)
        {
            return "{" + nameSpace + "}" + localName;
        }

        #endregion Private Methods
    }

    #region Ast Classes

    class SMState
    {
        public String Id;
        public IActivity Entry = new Empty();
        public IActivity Exit = new Empty();
        public bool Final;
        public IList<SMTransition> Transitions = new List<SMTransition>();
    }

    class SMTransition
    {
        public IActivity Action = new Empty();
        public String To;
        public bool Flag;
        public bool Condition;
    }

    class SMTrigger
    {
        public IActivity Trigger;
        public IList<SMTransition> Transitions = new List<SMTransition>();
    }

    #endregion Ast Classes
}
