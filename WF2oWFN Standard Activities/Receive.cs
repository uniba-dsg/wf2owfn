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
    /// Receive Activity
    /// Consult Xaml reference in documentation for structural details. 
    /// </summary>
    public class Receive : IPrimitive
    {
        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly String name = "Receive";
        private readonly String ns = "http://schemas.microsoft.com/netfx/2009/xaml/servicemodel";
        private String contract = "C";
        private String operation;

        public IActivity Parse(Queue<IXamlElement> token)
        {
            log.Debug(String.Format("Calling parse in activity '{0}'", name));

            // Start Element
            IXamlElement element = token.Dequeue();

            // Naming
            operation = element.Attributes["OperationName"];
            string cTemp;
            if (element.Attributes.TryGetValue("ServiceContractName", out cTemp))
            {
                string[] cArr = cTemp.Split(':');

                if (cArr.Length > 1)
                {
                    contract = cArr[1];
                }
                else
                {
                    contract = cArr[0];
                }
            }

            // Ignore inner structure
            while (!token.Peek().QName.Equals(this.QName))
            {
                token.Dequeue();
            }
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
            // Place exists?
            Place message = phylum.Find("out." + contract + "." + operation);
            if (message == null)
            {
                message = phylum.NewPlace("out." + contract + "." + operation, PetriNet.CommunicationType.Input);
            }
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
