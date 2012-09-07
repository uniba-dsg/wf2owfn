//----------------------------------------------------------------
// Copyright (c) Stefan Kolb.  2012.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace WF2oWFN.API
{
    /// <summary>
    /// Represents a Xaml(x)-tag representation for parsing an <c>IActivity</c> AST
    /// </summary>
    public interface IXamlElement
    {
        /// <summary>
        /// Qualified name of the element (e.g. {http://tempuri.org}Name)
        /// </summary>
        String QName
        {
            get;
        }

        /// <summary>
        /// Local Name of the element (e.g. Name)
        /// </summary>
        String LocalName
        {
            get;
            set;
        }

        /// <summary>
        /// Namespace of the element (e.g. http://tempuri.org)
        /// </summary>
        String Namespace
        {
            get;
            set;
        }

        /// <summary>
        /// Attributes of the element (e.g. {"DisplayName" "Pick"})
        /// </summary>
        IDictionary<String, String> Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the element is a closing tag
        /// </summary>
        bool IsClosingElement
        {
            get;
            set;
        }
    }
}
