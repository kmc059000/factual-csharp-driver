using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FactualDriver
{
    /// <summary>
    /// Class holder of multiple queries to be sent as one request.
    /// </summary>
    public class MultiQuery
    {
        /// <summary>
        /// Collection of query keys and full url paths.
        /// </summary>
        Dictionary<string, string> _queries = new Dictionary<string, string>();

        /// <summary>
        /// We start query count at one per driver documentation.
        /// </summary>
        private int _queryCount = 0;

        private string _key = "q";
        public string Key
        {
            get { return _key; } 
            set { _key = value; }
        }
        /// <summary>
        /// Add a a query to the multiquery collection
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query"></param>
        public MultiQuery AddQuery(string path, string query)
        {
            _queries.Add(Key + _queryCount, string.Format("{0}?{1}", path, query));
            _queryCount++;
            return this;
        }

        /// <summary>
        /// Converts multi query into a url encoded string.
        /// </summary>
        /// <returns></returns>
        public string ToUrlQuery()
        {
            var queries = string.Join(",", _queries.Select(p => string.Format("\"{0}\":\"/{1}\"", p.Key, p.Value)));
            return "queries=" + WebUtility.UrlEncode("{" + queries + "}");
        }
    }
}