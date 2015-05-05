using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace FactualDriver
{
    /// <summary>
    /// Holds all parameters for this ResolveQuery.
    /// </summary>
    public class ResolveQuery
    {
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private bool isDebugModeEnabled = false;

        /// <summary>
        /// Adds key and value to the resolve query list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ResolveQuery Add(string key, object value)
        {
            if (value is double)
                value = string.Format("{0:N2}", Convert.ToDouble(value));

            if (_parameters.ContainsKey(key))
            {
                _parameters[key] = value;
            }
            else
            {
                _parameters.Add(key, value);
            }
            return this;
        }


        /// <summary>
        /// When debug mode is turned on, Resolve will return all potential candidates for resolution
        /// and meta-data regarding resolution quality. When debug is turned off (the default), Resolve
        /// will return 0 results (meaning no match), or 1 result (meaning that was the unequivocal match).
        /// </summary>
        /// <param name="enableDebugMode"></param>
        /// <returns></returns>
        public ResolveQuery EnableDebugMode(bool enableDebugMode)
        {
            isDebugModeEnabled = enableDebugMode;
            return this;
        }

        /// <summary>
        /// Converts ResolveQuery object into url encoded string
        /// </summary>
        /// <returns></returns>
        public string ToUrlQuery()
        {
            return string.Format("{0}={1}&debug={2}", Constants.RESOLVE_VALUES,
                WebUtility.UrlEncode(JsonConvert.SerializeObject(_parameters)), 
                isDebugModeEnabled ? "true" : "false");
        }
    }
}