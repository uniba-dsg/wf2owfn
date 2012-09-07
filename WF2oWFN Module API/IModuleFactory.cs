//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WF2oWFN.API
{
    /// <summary>
    /// Loads plugin modules and creates <c>IActivity</c> instances for parsing and compiling Xaml(x) elements
    /// </summary>
    public interface IModuleFactory
    {
        /// <summary>
        /// Tries to create an instance of a module for the specified activity
        /// </summary>
        /// <param name="qname">The qualified name of the activity</param>
        /// <returns>An instance of an <c>IActivity</c> module for the given activity or <c>null</c> if no module was found</returns>
        IActivity CreateActivity(String qname);
    }
}
