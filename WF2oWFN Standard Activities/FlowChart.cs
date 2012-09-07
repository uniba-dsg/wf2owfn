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
    /// FlowChart Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    public class FlowChart : IComposite
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Flowchart";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/activities";
        private readonly String ns_x = "http://schemas.microsoft.com/winfx/2006/xaml";
        // Module Factory
        private IModuleFactory moduleFactory;
        // AST
        private String startNode;
        private IList<FlowNode> steps = new List<FlowNode>();
        // TokenCount
        private int initialTokenCount;

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));
            // Debug
            initialTokenCount = token.Count;

            // [Start] FlowChart
            IXamlElement root = token.Dequeue();

            // [Attr] StartNode
            String start;
            if (root.Attributes.TryGetValue("StartNode", out start))
            {
                startNode = extractReference(start);
            }

            while (!token.Peek().QName.Equals(this.QName) || !token.Peek().IsClosingElement)
            {
                if(token.Peek().QName.Equals(createQName("Flowchart.StartNode")))
                {
                    // [Start] StartNode
                    token.Dequeue();

                    while (!token.Peek().QName.Equals(createQName("Flowchart.StartNode")))
                    {
                        if (token.Peek().QName.Equals(createQName("Reference", ns_x)))
                        {
                            IXamlElement elem = token.Dequeue();
                            String reference;
                            elem.Attributes.TryGetValue("Reference", out reference);
                            startNode = reference;
                            token.Dequeue();
                        }
                        else
                        {
                            if (startNode == null)
                            {
                                if (token.Peek().QName.Equals(createQName("Null", ns_x)))
                                {
                                    token.Dequeue();
                                    token.Dequeue();
                                }
                                else
                                {
                                    startNode = token.Peek().Attributes["x:Name"];
                                    parseNode(token);
                                }
                            }
                        }
                    }
                    // [End] StartNode
                    token.Dequeue();
                }
                else
                {
                    parseNode(token);
                }
            }
            // [End] FlowChart
            token.Dequeue();

            return this;
        }

        public PetriNet Compile(PetriNet phylum)
        {
            String prefix = phylum.ActivityCount + ".internal.";

            Place p1 = phylum.NewPlace(prefix + "initialized");
            Place p2 = phylum.NewPlace(prefix + "closed");

            if (startNode == null)
            {
                // New Activity
                phylum.ActivityCount += 1;
                int currentID = phylum.ActivityCount;
                // Compile
                new Empty().Compile(phylum);
                // Merge
                phylum.Merge(p1, currentID + ".internal.initialized");
                phylum.Merge(p2, currentID + ".internal.closed");

                return phylum;
            }

            // Create Steps
            foreach (FlowNode node in steps)
            {
                if (node is FlowStep)
                {
                    FlowStep step = (FlowStep)node;
                    String prefixStep = prefix + step.Id + ".";

                    Place stp1 = phylum.NewPlace(prefixStep + "start");
                    Place stp2 = phylum.NewPlace(prefixStep + "next");

                    phylum.ActivityCount += 1;
                    int currentID = phylum.ActivityCount;
                    // Action Activity
                    step.Action.Compile(phylum);
                    // Connect
                    phylum.Merge(stp1, currentID + ".internal.initialized");
                    phylum.Merge(stp2, currentID + ".internal.closed");
                }
                else if (node is FlowDecision)
                {
                    FlowDecision dec = (FlowDecision)node;
                    String prefixStep = prefix + dec.Id + ".";

                    Place dp1 = phylum.NewPlace(prefixStep + "start");
                    Place condition = phylum.NewPlace(prefixStep + "condition");
                    Place right = phylum.NewPlace(prefixStep + "true");
                    Place wrong = phylum.NewPlace(prefixStep + "false");
                    Transition evalCondition = phylum.NewTransition(prefixStep + "evalcondition");
                    Transition startTrue = phylum.NewTransition(prefixStep + "starttrue");
                    Transition startFalse = phylum.NewTransition(prefixStep + "startfalse");
                    // Connect
                    phylum.NewArc(dp1, evalCondition);
                    phylum.NewArc(evalCondition, condition);
                    phylum.NewArc(condition, startTrue);
                    phylum.NewArc(condition, startFalse);
                    phylum.NewArc(startTrue, right);
                    phylum.NewArc(startFalse, wrong);
                }
                else if (node is FlowSwitch)
                {
                    FlowSwitch swtch = (FlowSwitch)node;
                    String prefixStep = prefix + swtch.Id + ".";

                    Place sp1 = phylum.NewPlace(prefixStep + "start");
                    Place sdefault = phylum.NewPlace(prefixStep + "default");

                    // Inner Petri Net
                    Place lastState = sp1;

                    for (int i = 1; i <= swtch.Branches.Count + 1; i++)
                    {
                        if (i <= swtch.Branches.Count)
                        {
                            Place cond = phylum.NewPlace(prefixStep + "condition" + i);
                            Place state = phylum.NewPlace(prefixStep + "case" + i);
                            Transition evalCase = phylum.NewTransition(prefixStep + "evalcase" + i);
                            Transition initCase = phylum.NewTransition(prefixStep + "initcase" + i);
                            phylum.NewArc(lastState, evalCase);
                            phylum.NewArc(evalCase, cond);
                            phylum.NewArc(cond, initCase);
                            phylum.NewArc(initCase, state);
                            // LastState
                            lastState = cond;
                        }
                        else
                        {
                            Transition initDefault = phylum.NewTransition(prefixStep + "initdefault");
                            phylum.NewArc(lastState, initDefault);
                            phylum.NewArc(initDefault, sdefault);
                        }
                    }
                }
            }
            // Merge
            // StartNode
            phylum.Merge(p1, prefix + steps.First(e => e.Id.Equals(startNode)).Id + ".start");

            foreach (FlowNode node in steps)
            {
                if (node is FlowStep)
                {
                    FlowStep step = (FlowStep)node;
                    String prefixStep = prefix + step.Id + ".";

                    if (step.Next != null)
                    {
                        phylum.Merge(prefixStep + "next", prefix + step.Next + ".start");
                    }
                    else
                    {
                        phylum.Merge(p2, prefixStep + "next");
                    }
                }
                else if (node is FlowDecision)
                {
                    FlowDecision dec = (FlowDecision)node;
                    String prefixDecision = prefix + dec.Id + ".";

                    if (dec.Right != null)
                    {
                        phylum.Merge(prefixDecision + "true", prefix + dec.Right + ".start");
                    }
                    else
                    {
                        phylum.Merge(p2, prefixDecision + "true");
                    }

                    if (dec.Wrong != null)
                    {
                        phylum.Merge(prefixDecision + "false", prefix + dec.Wrong + ".start");
                    }
                    else
                    {
                        phylum.Merge(p2, prefixDecision + "false");
                    }
                }
                else if (node is FlowSwitch)
                {
                    FlowSwitch swtch = (FlowSwitch)node;
                    String prefixSwitch = prefix + swtch.Id + ".";

                    //Default
                    if (swtch.Default != null)
                    {
                        phylum.Merge(prefixSwitch + "default", prefix + swtch.Default + ".start");
                    }
                    else
                    {
                        phylum.Merge(p2, prefixSwitch + "default");
                    }
                    // Branches
                    for(int i=1; i<= swtch.Branches.Count; i++)
                    {
                        phylum.Merge(prefixSwitch + "case" + i, prefix + swtch.Branches[i-1] + ".start");
                    }
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

        private String parseNode(Queue<IXamlElement> token)
        {
            if (token.Peek().QName.Equals(createQName("FlowStep")))
            {
                return parseStep(token);
            }
            else if (token.Peek().QName.Equals(createQName("FlowDecision")))
            {
                return parseDecision(token);
            }
            else if (token.Peek().QName.Equals(createQName("FlowSwitch")))
            {
                return parseSwitch(token);
            }
            else if (token.Peek().QName.Equals(createQName("Reference", ns_x)))
            {
                IXamlElement elem = token.Dequeue();
                // Reference
                String reference;
                elem.Attributes.TryGetValue("Reference", out reference);
                // Optional <x:Key>
                if (token.Peek().QName.Equals(createQName("Key", ns_x)))
                {
                    token.Dequeue();
                    token.Dequeue();
                }
                token.Dequeue();

                return reference;
            }
            else
            {
                throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
            }
        }

        private String parseStep(Queue<IXamlElement> token)
        {
            FlowStep step = new FlowStep();
            // [Start] FlowStep
            IXamlElement elem = token.Dequeue();
            // Reference
            String reference;
            elem.Attributes.TryGetValue("x:Name", out reference);
            step.Id = reference;

            while (!token.Peek().QName.Equals(createQName("FlowStep")))
            {
                // Optional Key Tag in Switch
                if (token.Peek().QName.Equals(createQName("Key", ns_x)))
                {
                    token.Dequeue();

                    while (!token.Peek().QName.Equals(createQName("Key", ns_x)))
                    {
                        token.Dequeue();
                    }

                    token.Dequeue();
                }

                // Activity
                IActivity activity = moduleFactory.CreateActivity(token.Peek().QName);

                if (activity != null)
                {
                    IActivity inner = activity.Parse(token);
                    step.Action = inner;
                }
                else
                {
                    throw new ParseException(String.Format("No Module found for activity '{0}'", QName), initialTokenCount - token.Count, QName);
                }
                // Optional FlowStep.Next
                if (token.Peek().QName.Equals(createQName("FlowStep.Next")))
                {
                    // [Start] FlowStep.Next
                    token.Dequeue();
                    step.Next = parseNode(token);
                    // [End] FlowStep.Next
                    token.Dequeue();
                }
            }
            // [End] FlowStep
            token.Dequeue();

            steps.Add(step);
            return step.Id;
        }

        private String parseDecision(Queue<IXamlElement> token)
        {
            FlowDecision dec = new FlowDecision();
            // [Start] FlowDecision
            IXamlElement elem = token.Dequeue();
            // Reference
            String reference;
            elem.Attributes.TryGetValue("x:Name", out reference);
            dec.Id = reference;
            // True
            String right;
            if (elem.Attributes.TryGetValue("True", out right))
            {
                dec.Right = extractReference(right);
            }
            // False
            String wrong;
            if (elem.Attributes.TryGetValue("False", out wrong))
            {
                dec.Wrong = extractReference(wrong);
            }

            while (!token.Peek().QName.Equals(createQName("FlowDecision")))
            {
                if (token.Peek().QName.Equals(createQName("FlowDecision.True")))
                {
                    token.Dequeue();
                    dec.Right = parseNode(token);
                    token.Dequeue();
                }
                else if (token.Peek().QName.Equals(createQName("FlowDecision.False")))
                {
                    token.Dequeue();
                    dec.Wrong = parseNode(token);
                    token.Dequeue();
                }
                else
                {
                    throw new ParseException(String.Format("Unknown tag found in activity '{0}'", QName), initialTokenCount - token.Count, QName);
                }
            }
            // [End] FlowDecision
            token.Dequeue();

            steps.Add(dec);

            return dec.Id;
        }

        private String parseSwitch(Queue<IXamlElement> token)
        {
            FlowSwitch swtch = new FlowSwitch();
            // [Start] FlowDecision
            IXamlElement elem = token.Dequeue();
            // Reference
            String reference;
            elem.Attributes.TryGetValue("x:Name", out reference);
            swtch.Id = reference;
            // Default
            String def;
            if (elem.Attributes.TryGetValue("Default", out def))
            {
                swtch.Default = extractReference(def);
            }

            while (!token.Peek().QName.Equals(createQName("FlowSwitch")))
            {
                if (token.Peek().QName.Equals(createQName("FlowSwitch.Default")))
                {
                    token.Dequeue();
                    swtch.Default = parseNode(token);
                    token.Dequeue();
                }
                else
                {
                    swtch.Branches.Add(parseNode(token));
                }
            }
            // [End] FlowDecision
            token.Dequeue();

            steps.Add(swtch);

            return swtch.Id;
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

    class FlowNode
    {
        public String Id;
    }

    class FlowStep : FlowNode
    {
        public IActivity Action;
        public String Next;
    }

    class FlowDecision : FlowNode
    {
        public String Right;
        public String Wrong;
    }

    class FlowSwitch : FlowNode
    {
        public String Default;
        public IList<String> Branches = new List<String>();
    }   

    #endregion Ast Classes
}
