using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace FactualDriver
{
    /// <summary>
    /// Holds all parameters for this MatchQuery.
    /// </summary>
    public class MatchQuery
    {
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();

        /// <summary>
        /// Adds key and value to the match query list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MatchQuery Add(string key, object value)
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
        /// Converts MatchQuery object into url encoded string
        /// </summary>
        /// <returns>string</returns>
        public string ToUrlQuery()
        {
            return string.Format("{0}={1}", Constants.MATCH_VALUES, WebUtility.UrlEncode(JsonConvert.SerializeObject(_parameters)));
        }
    }
}