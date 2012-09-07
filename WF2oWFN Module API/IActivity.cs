//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WF2oWFN.API;
using WF2oWFN.API.Petri;

namespace WF2oWFN.API
{
    /// <summary>
    /// Represents all possible Xaml(x)-Activities.
    /// </summary>
    public interface IActivity
    {
        /// <summary>
        /// Parse the Xaml(x)-Activity into an AST
        /// </summary>
        /// <param name="token">A Queue containing all IXamlElements which shall be parsed</param>
        /// <returns>An <c>IActivity</c> AST representation </returns>
        IActivity Parse(Queue<IXamlElement> token);

        /// <summary>
        /// Compile the <c>IActivity</c> into a Petri Net
        /// </summary>
        /// <param name="phylum">The phylum net that may already contain structures</param>
        /// <returns>A petri net representation of the <c>IActivity</c> added to the <c>phylum</c> net</returns>
        PetriNet Compile(PetriNet phylum);

        /// <summary>
        /// Getter for the Activitiy's QualifiedName.
        /// </summary>
        String QName
        {
            get;
        }
        
        /// <summary>
        /// Getter for the  Activitiy's LocalName.
        /// </summary>
        String LocalName
        {
            get;
        }
    }
}
