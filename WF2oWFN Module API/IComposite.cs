//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WF2oWFN.API;

namespace WF2oWFN.API
{
    /// <summary>
    /// Represents structured Xaml(x)-Activities that may have inner activities.
    /// </summary>
    public interface IComposite : IActivity
    {
        /// <summary>
        /// Sets the IModuleFactory for handling inner activities.
        /// </summary>
        IModuleFactory ModuleFactory
        {
            set;
        }
    }
}
