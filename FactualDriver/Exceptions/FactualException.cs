using System;
using System.Runtime.Serialization;

namespace FactualDriver.Exceptions
{
    /// <summary>
    /// A generic Factual exception class
    /// </summary>
#if PORTABLE
    [DataContract]
#else
    [Serializable]
#endif
    public class FactualException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public FactualException(string message) : base(message) {}
    }
}