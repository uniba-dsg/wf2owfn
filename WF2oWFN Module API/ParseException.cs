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
    /// Custom exception for handling parse exceptions in <c>IActivity</c> modules
    /// </summary>
    public class ParseException : ApplicationException
    {
        // Exception message
        private string message;
        // IActivity name which caused the exception
        private string activity;
        // Element number in the IXamlElement queue where the exception happened 
        private int elementNumber;

        /// <summary>
        /// Constructor
        /// </summary>
        public ParseException() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public ParseException(string message)
        {
            this.message = message;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="elementNumber">Element number in the <c>IXamlElement</c> queue where the exception happened</param>
        /// <param name="activity"><c>IActivity</c> name which caused the exception</param>
        public ParseException(string message, int elementNumber, string activity)
        {
            this.message = message;
            this.elementNumber = elementNumber;
            this.activity = activity;
        }

        /// <summary>
        /// Property for the element number in the <c>IXamlElement</c> queue where the exception happened 
        /// </summary>
        public int ElementNumber
        {
            get { return this.elementNumber; }
            set { this.elementNumber = value; }
        }

        /// <summary>
        /// Property for the <c>IActivity</c> name which caused the exception
        /// </summary>
        public string Activity
        {
            get { return this.activity; }
            set { this.activity = value; }
        }

        /// <summary>
        /// Property for the exception message
        /// </summary>
        public override string Message
        {
            get { return this.message; }
        }
    }
}
