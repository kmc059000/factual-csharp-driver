using System;
using System.Net;
using System.Runtime.Serialization;

namespace FactualDriver.Exceptions
{
    /// <summary>
    /// Factaul api exception class, represent an error received from the Factual API.
    /// </summary>

#if PORTABLE
    [DataContract]
#else
    [Serializable]
#endif
    public class FactualApiException : Exception 
    {
        /// <summary>
        /// Status code of the http response from Factual API.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
        /// <summary>
        /// Text response that was received with an error.
        /// </summary>
        public string Response { get; set; }
        /// <summary>
        /// Url which was requested from Factual API, for troubleshooting purposes.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Constructor for exception.
        /// </summary>
        /// <param name="statusCode">HttpStatusCode</param>
        /// <param name="response">Factual response string received from API.</param>
        /// <param name="url">Url requested from Factual which caused an error.</param>
        public FactualApiException(HttpStatusCode statusCode, string response, string url)
        {
            StatusCode = statusCode;
            Response = response;
            Url = url;
        }

#if !PORTABLE
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
#endif
    }
}