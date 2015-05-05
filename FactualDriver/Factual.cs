using FactualDriver.Exceptions;
using FactualDriver.Filters;
using FactualDriver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using OAuth2LeggedAuthenticator = FactualDriver.OAuth.OAuth2LeggedAuthenticator;

namespace FactualDriver
{
    /// <summary>
    /// Main point of entry for the driver. Supports running queries against Factual
    /// and inspecting the response. Supports the same levels of authentication
    /// supported by Factual's API.
    /// </summary>
    public class Factual
    {
        private readonly OAuth2LeggedAuthenticator _factualAuthenticator;
        private const string DriverHeaderTag = "factual-csharp-driver-v1.6.6";
        private MultiQuery _multiQuery;
        public int? ConnectionTimeout { get; set; }
        public int? ReadTimeout { get; set; }
        public string FactualApiUrlOverride { get; set; }

        /// <summary>
        /// MultiQuery accessor. Creates and returns new instance of MultiQuery if one already doesn't exists.
        /// </summary>
        public MultiQuery MultiQuery
        {
            get { return _multiQuery ?? (_multiQuery = new MultiQuery()); }
        }

        /// <summary>
        /// Set the driver in or out of debug mode. True to display in the output window.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Create an instance of Factual .NET driver
        /// </summary>
        /// <param name="oAuthKey">OAuth consumer key</param>
        /// <param name="oAuthSecret">Oauth consumer secret key</param>
        public Factual(string oAuthKey, string oAuthSecret)
        {
            _factualAuthenticator = new OAuth2LeggedAuthenticator(oAuthKey, oAuthSecret);
        }

        /// <summary>
        /// Create an instance of Factual .NET driver
        /// </summary>
        /// <param name="oAuthKey">OAuth consumer key</param>
        /// <param name="oAuthSecret">Oauth consumer secret key</param>
        /// <param name="debug">Include debuggin info</param>
        public Factual(string oAuthKey, string oAuthSecret, bool debug)
        {
            _factualAuthenticator = new OAuth2LeggedAuthenticator(oAuthKey, oAuthSecret);
            Debug = debug;
        }

        /// <summary>
        /// Create a new Factual WebRequest for granual control  
        /// </summary>
        /// <param name="httpMethod">Http method name, GET, POST, etc</param>
        /// <param name="query">Relative path string with factual parameters</param>
        /// <returns></returns>
        public HttpWebRequest CreateWebRequest(string httpMethod, string query)
        {
            string factualApiUrl = string.IsNullOrEmpty(FactualApiUrlOverride) ? "http://api.v3.factual.com" : FactualApiUrlOverride;
            var requestUrl = new Uri(new Uri(factualApiUrl), query);
            var request = _factualAuthenticator.CreateHttpWebRequest(httpMethod, requestUrl);
            request.Headers.Add("X-Factual-Lib", DriverHeaderTag);
            request.Timeout = ConnectionTimeout ?? 100000;
            request.ReadWriteTimeout = ReadTimeout ?? 300000;
            return request;
        }

        /// <summary>
        /// Execute a path against a factual api with Filter Parameters and return a json string
        /// </summary>
        /// <param name="query">Api address of the request</param>
        /// <param name="filters">List of parameter filters against the api</param>
        /// <returns></returns>
        public string Query(string query, IEnumerable<IFilter> filters)
        {
            return RawQuery(query, JsonUtil.ToQueryString(filters));
        }

        /// <summary>
        /// Execute a path against a factual api with Filter Parameters and return a json string
        /// </summary>
        /// <param name="query">Api address of the request</param>
        /// <param name="filters">List of parameter filters against the api</param>
        /// <returns></returns>
        public string Query(string query, params IFilter[] filters)
        {
            return Query(query, (IEnumerable<IFilter>)filters);
        }

        /// <summary>
        /// Ask Factual to match the entity for the attributes specified by MatchQuery
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="query"></param>
        /// <returns>the response of running a match query against Factual.</returns>
        public string Match(string tableName, MatchQuery query)
        {
            var response = RawQuery(UrlForMatch(tableName), query.ToUrlQuery());
            dynamic json = JsonConvert.DeserializeObject(response);
            if (((int) json.response.included_rows) == 1)
                return (string) json.response.data[0].factual_id;
            else
                return null;
        }

        /// <summary>
        /// Runs a read query against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to query (e.g., "places")</param>
        /// <param name="query">the read query to run against table.</param>
        /// <returns>the response of running query against Factual.</returns>
        public string Fetch(string tableName, Query query)
        {
            return RawQuery(UrlForFetch(tableName),query.ToUrlQuery());
        }

        /// <summary>
        /// Asks Factual to resolve the entity for the attributes specified by
        /// query, within the table called tableName.
        /// Returns the read response from a Factual Resolve request, which includes
        /// all records that are potential matches.
        /// Each result record will include a confidence score ("similarity"),
        /// and a flag indicating whether Factual decided the entity is the correct
        /// resolved match with a high degree of accuracy ("resolved").
        /// There will be 0 or 1 entities returned with "resolved"=true. If there was a
        /// full match, it is guaranteed to be the first record in the response.
        /// </summary>
        /// <param name="tableName">the name of the table to resolve within.</param>
        /// <param name="query">a Resolve query with partial attributes for an entity.</param>
        /// <returns>the response from Factual for the Resolve request.</returns>
        public string Fetch(string tableName, ResolveQuery query)
        {
            return RawQuery(UrlForResolve(tableName), query.ToUrlQuery());
        }

        /// <summary>
        /// Runs a facet read against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to query for facets (e.g., "places")</param>
        /// <param name="query">the facet query to run against table</param>
        /// <returns>the response of running facet against Factual.</returns>
        public string Fetch(string tableName, FacetQuery query)
        {
            return RawQuery(UrlForFacets(tableName), query.ToUrlQuery());
        }

        /// <summary>
        /// Runs a diff query against the specified Factual table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        public string Fetch(string tableName, DiffsQuery diff)
        {
            return RawQuery(UrlForDiffs(tableName), diff.ToUrlQuery());
        }

        /// <summary>
        /// Runs a row query against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to query (e.g., "places")</param>
        /// <param name="factualId">the factual id (e.g., "03c26917-5d66-4de9-96bc-b13066173c65")</param>
        /// <param name="query">the row query to run against table.</param>
        /// <returns>the response of running query against Factual.</returns>
        public string FetchRow(string tableName, string factualId, RowQuery query)
        {
            return RawQuery(UrlForFetchRow(tableName, factualId), query.ToUrlQuery());
        }

        /// <summary>
        /// Runs a row query against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to query (e.g., "places")</param>
        /// <param name="factualId">the factual id (e.g., "03c26917-5d66-4de9-96bc-b13066173c65")</param>
        /// <returns>the response of running query against Factual.</returns>
        public string FetchRow(string tableName, string factualId)
        {
            return FetchRow(tableName, factualId, new RowQuery());
        }

        /// <summary>
        /// Runs a Submit input against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to submit updates for (e.g., "places")</param>
        /// <param name="submit">the submit parameters to run against table</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response of running submit against Factual.</returns>
        public string Submit(string tableName, Submit submit, Metadata metadata)
        {
            return SubmitCustom("t/" + tableName + "/submit", submit, metadata);
        }

        /// <summary>
        /// Runs a Submit input against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to submit updates for (e.g., "places")</param>
        /// <param name="factualId">the factual id on which the submit is run</param>
        /// <param name="submit">the submit parameters to run against table</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response of running submit against Factual.</returns>
        public string Submit(string tableName, string factualId, Submit submit, Metadata metadata)
        {
            return SubmitCustom("t/" + tableName + "/" + factualId + "/submit", submit, metadata);
        }

        private string SubmitCustom(string root, Submit submit, Metadata metadata)
        {
            var postData = submit.ToUrlQuery() + "&" + metadata.ToUrlQuery();
            return RequestPost(root, postData, "");
        }

        /// <summary>
        /// Runs a clear request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to clear attributes for an entity (e.g., "places")</param>
        /// <param name="factualId">the factual id on which the clear is run</param>
        /// <param name="clear">the clear parameters to run against entity</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response of running clear request on a Factual entity.</returns>
        public string Clear(string tableName, string factualId, Clear clear, Metadata metadata)
        {
            return ClearCustom("t/" + tableName + "/" + factualId + "/clear", clear, metadata);
        }

        private string ClearCustom(string root, Clear clear, Metadata metadata)
        {
            var postData = clear.ToUrlQuery() + "&" + metadata.ToUrlQuery();
            return RequestPost(root, postData, "");
        }

        /// <summary>
        /// Runs a boost post request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to boost attributes for an entity (e.g., "places").</param>
        /// <param name="factualId">the factual id on which the boost is run.</param>
        /// <returns>the response of running boost post request on a Factual entity.</returns>
        public string Boost(string tableName, string factualId)
        {
            return Boost(tableName, new Boost(factualId));
        }

        /// <summary>
        /// Runs a boost post request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to boost attributes for an entity (e.g., "places").</param>
        /// <param name="factualId">the factual id on which the boost is run.</param>
        /// <param name="search">the full text search string on which the boost is run.</param>
        /// <returns>the response of running boost post request on a Factual entity.</returns>
        public string Boost(string tableName, string factualId, string search)
        {
          return Boost(tableName, new Boost(factualId).Search(search));
        }

        /// <summary>
        /// Runs a boost post request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to boost attributes for an entity (e.g., "places").</param>
        /// <param name="factualId">the factual id on which the boost is run.</param>
        /// <param name="search">the full text search string on which the boost is run.</param>
        /// <param name="user">the user which executes the boost.</param>
        /// <returns>the response of running boost post request on a Factual entity.</returns>
        public string Boost(string tableName, string factualId, string search, string user)
        {
          return Boost(tableName, new Boost(factualId).Search(search).User(user));
        }

        /// <summary>
        /// Runs a boost post request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to boost attributes for an entity (e.g., "places").</param>
        /// <param name="factualId">the factual id on which the boost is run.</param>
        /// <param name="query">the row query to run against table.</param>
        /// <param name="user">the metadata which executes the boost.</param>
        /// <returns>the response of running boost post request on a Factual entity.</returns>
        public string Boost(string tableName, string factualId, Query query, Metadata user)
        {
          return Boost(tableName, new Boost(factualId, query, user));
        }

        /// <summary>
        /// Runs a boost post request of existing attributes on a Factual entity.
        /// </summary>
        /// <param name="tableName">the name of the table in which to boost attributes for an entity (e.g., "places").</param>
        /// <param name="boost">the boost to perform on a Factual entity.</param>
        /// <returns>the response of running boost post request on a Factual entity.</returns>
        public string Boost(string tableName, Boost boost)
        {
            return RequestPost("t/" + tableName + "/boost", boost.ToUrlQuery(), "");
        }

        /// <summary>
        /// Run a schema query against the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to query (e.g., "places")</param>
        /// <returns>the response of running query against Factual.</returns>
        public string Schema(string tableName)
        {
            return RawQuery(UrlForSchema(tableName));
        }

        /// <summary>
        /// Run a Geocode query against the Factual API.
        /// <para name="Example"> This example shows common usage: </para>
        /// <c>new Point(34.06021, -118.41828)</c>
        /// </summary>
        /// <param name="point">Point to get geocode</param>
        /// <returns>the response of running query against Factual.</returns>
        public string ReverseGeocode(Point point)
        {
            return RawQuery(UrlForGeocode(), point.ToUrlQuery());
        }

        /// <summary>
        /// Queue a raw read request for inclusion in the next multi request.
        /// </summary>
        /// <param name="path">the path to run the request against</param>
        /// <param name="query">the parameters to send with the request</param>
        public void QueueFetch(string path, string query)
        {
            MultiQuery.AddQuery(path, query);
        }

        /// <summary>
        /// Queue a read request for inclusion in the next multi request.
        /// </summary>
        /// <param name="table">the name of the table you wish to query (e.g., "places")</param>
        /// <param name="query">the read query to run against table.</param>
        public void QueueFetch(string table, Query query)
        {
            MultiQuery.AddQuery(UrlForFetch(table), query.ToUrlQuery());
        }

        /// <summary>
        /// Queue a resolve request for inclusion in the next multi request.
        /// </summary>
        /// <param name="table">the name of the table you wish to use resolve against (e.g., "places")</param>
        /// <param name="query">the resolve query to run against table.</param>
        public void QueueFetch(string table, ResolveQuery query)
        {
            MultiQuery.AddQuery(UrlForResolve(table), query.ToUrlQuery());
        }

        /// <summary>
        /// Queue a facet request for inclusion in the next multi request.
        /// </summary>
        /// <param name="table">the name of the table you wish to use a facet request against (e.g., "places")</param>
        /// <param name="query">the facet query to run against table.</param>
        public void QueueFetch(string table, FacetQuery query)
        {
            MultiQuery.AddQuery(UrlForFacets(table), query.ToUrlQuery());
        }

        /// <summary>
        /// Queue a ReverseGeocode for inclusion in the next multi request.
        /// </summary>
        /// <param name="point">the geo location point parameter</param>
        public void QueueFetch(Point point)
        {
            MultiQuery.AddQuery(UrlForGeocode(), point.ToUrlQuery());
        }

        /// <summary>
        /// Send all milti query requests which were queued up.
        /// </summary>
        /// <returns></returns>
        public string SendQueueRequests()
        {
            return RawQuery(UrlForMulti(), MultiQuery.ToUrlQuery());
        }

        protected static string UrlForResolve(string tableName)
        {
            return "t/" + tableName + "/resolve";
        }

        protected static string UrlForMatch(string tableName)
        {
            return "t/" + tableName + "/match";
        }

        protected static string UrlForFetch(string tableName)
        {
            return "t/" + tableName;
        }

        protected static string UrlForDiffs(string tableName)
        {
            return "t/" + tableName + "/diffs";
        }

        protected static string UrlForFetchRow(string tableName, string factualId)
        {
            return "t/" + tableName + "/" + factualId;
        }

        protected static string UrlForFacets(string tableName)
        {
            return "t/" + tableName + "/facets";
        }

        protected static string UrlForGeocode()
        {
            return "places/geocode";
        }

        protected static string UrlForSchema(string tableName)
        {
            return "t/" + tableName + "/schema";
        }

        protected static string UrlForMulti()
        {
            return "multi";
        }

        protected static string UrlForFlag(string tableName, string factualId)
        {
            return "t/" + tableName + "/" + factualId + "/flag";
        }

        /// <summary>
        /// Flags a row as a duplicate in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        [Obsolete("Deprecated method for flagging a duplicate. Please use the newer method which takes a 'preferredFactualId' instead")]
        public string FlagDuplicate(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "duplicate", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as a duplicate in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="preferredFactualId">the factual id that is preferred of the two duplicates to persist</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagDuplicate(string tableName, string factualId, String preferredFactualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "duplicate", preferredFactualId, null, metadata);
        }

        /// <summary>
        /// Flags a row as having been relocated, where its new location is an existing record,
        /// identified by preferredFactualid. If there is no record corresponding to the relocated
        /// business, use the submit API to update the record's address instead.
        /// </summary>
        /// <param name="tablename">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">>the factual id that is the relocated</param>
        /// <param name="preferredFactualId">the factual id that is preferred.</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns></returns>
        public String FlagRelocated(string tablename, string factualId, String preferredFactualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tablename, factualId), "relocated", preferredFactualId, null, metadata);
        }

        /// <summary>
        /// Flags a row as inaccurate in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        [Obsolete("Deprecated method for flagging a row as inaccurate. Please use the newer method which takes a List of inaccurate field names instead.")]
        public string FlagInaccurate(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "inaccurate", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as inaccurate in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="fields">A List of fields(by name) which you know to contain inaccurate data,
        /// however for which you don't actually have the proper corrections. If you have actual corrections,
        /// please use the submit API to update the row.</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagInaccurate(string tableName, string factualId, List<String> fields, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "inaccurate", null, fields, metadata);
        }

        /// <summary>
        /// Flags a row as inappropriate in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagInappropriate(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "inappropriate", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as non-existent in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagNonExistent(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "nonexistent", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as spam in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagSpam(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "spam", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as closed in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table that contains the item you wish to flag as closed (e.g., "places")</param>
        /// <param name="factualId">the factual id of the item you wish to flag as closed</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging an item as closed.</returns>
        public string FlagClosed(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "closed", null, null, metadata);
        }

        /// <summary>
        /// Flags a row as problematic in the specified Factual table.
        /// </summary>
        /// <param name="tableName">the name of the table you wish to flag a duplicate for (e.g., "places")</param>
        /// <param name="factualId">the factual id that is the duplicate</param>
        /// <param name="metadata">the metadata to send with information on this request</param>
        /// <returns>the response from flagging a duplicate row.</returns>
        public string FlagOther(string tableName, string factualId, Metadata metadata)
        {
            return FlagCustom(UrlForFlag(tableName, factualId), "other", null, null, metadata);
        }

        private string FlagCustom(string root, string flagType, String preferredFactualId, List<String> fields, Metadata metadata)
        {
            var postData = "problem=" + flagType + "&" + metadata.ToUrlQuery();

            if (preferredFactualId != null)
            {
                postData += "&preferred=" + preferredFactualId;
            }

            if (fields != null && fields.Count > 0)
            {
                postData += "&fields=" + WebUtility.UrlEncode(JsonConvert.SerializeObject(fields));
            }

            return RequestPost(root, postData, "");
        }

        /// <summary>
        /// Runs a GET request against the specified endpoint path, using the given
        /// parameters and your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to path. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// </summary>
        /// <param name="path">The endpoint path to run the request against. Example: "t/places"</param>
        /// <param name="queryParameters">The query string parameters to send with the request. Do not encode
        /// or escape these; that will be done automatically</param>
        /// <returns>The response body from running the GET request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RawQuery(string path, Dictionary<string, object> queryParameters)
        {
            return RawQuery(string.Format("{0}?{1}", path, UrlForRaw(queryParameters)));
        }

        /// <summary>
        /// Runs a GET request against the specified endpoint path, using the given
        /// parameters and your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to path. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// Developer is entirly responsible for correct query formatting and URL encoding.
        /// </summary>
        /// <param name="path">The endpoint path to run the request against. Example: "t/places"</param>
        /// <param name="queryParameters">The query string parameters to send with the request</param>
        /// <returns>The response body from running the GET request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RawQuery(string path, string queryParameters)
        {
            return RawQuery(string.Format("{0}?{1}", path, queryParameters));
        }

        /// <summary>
        /// Runs a GET request against the specified complete path with query,
        /// using your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to completePathWithQuery. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// Developer is entirly responsible for correct query formatting and URL encoding.
        /// </summary>
        /// <param name="completePathWithQuery">The complete path with query to run the request against.
        /// Example: "t/places-us/diffs?start=1354916463822&end=1354917903834"</param>
        /// <returns>The response body from running the GET request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RawQuery(string completePathWithQuery)
        {
            var request = CreateWebRequest("GET", completePathWithQuery);
            if (Debug)
            {
                System.Diagnostics.Trace.WriteLine("==== Driver Version ====");
                System.Diagnostics.Trace.WriteLine(DriverHeaderTag);
                System.Diagnostics.Trace.WriteLine("==== Connection Timeout ====");
                System.Diagnostics.Trace.WriteLine(request.Timeout);
                System.Diagnostics.Trace.WriteLine("==== Read/Write Timeout ====");
                System.Diagnostics.Trace.WriteLine(request.ReadWriteTimeout);
                System.Diagnostics.Trace.WriteLine("\n\n==== Request Headers ====");
                System.Diagnostics.Trace.WriteLine(request.Headers);
                System.Diagnostics.Trace.WriteLine("==== Request Method ====");
                System.Diagnostics.Trace.WriteLine(request.Method);
                System.Diagnostics.Trace.WriteLine("==== Request Url ====");
                System.Diagnostics.Trace.WriteLine(request.RequestUri);
            }
            return ReadRequest(completePathWithQuery, request);
        }

        /// <summary>
        /// Runs a POST request against the specified endpoint path, using the given
        /// parameters and your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to path. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// </summary>
        /// <param name="path">The endpoint path to run the request against. Example: "t/places"</param>
        /// <param name="queryParameters">The query string parameters to send with the request. Do not encode
        /// or escape these; that will be done automatically</param>
        /// <param name="postData">The POST content parameters to send with the request. Do not encode
        /// or escape these; that will be done automatically</param>
        /// <returns>The response body from running the POST request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RequestPost(string path, Dictionary<string, object> queryParameters, Dictionary<string, object> postData)
        {
            return RequestPost(string.Format("{0}?{1}", path, UrlForRaw(queryParameters)), UrlForRaw(postData));
        }

        /// <summary>
        /// Runs a POST request against the specified endpoint path, using the given
        /// parameters and your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to path. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// Developer is entirly responsible for correct query formatting and URL encoding.
        /// </summary>
        /// <param name="path">The endpoint path to run the request against. Example: "t/places"</param>
        /// <param name="queryParameters">The query string parameters to send with the request</param>
        /// <param name="postData">The POST content parameters to send with the request</param>
        /// <returns>The response body from running the POST request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RequestPost(string path, string queryParameters, string postData)
        {
            return RequestPost(string.Format("{0}{1}{2}", path, string.IsNullOrEmpty(queryParameters) ? "" : "?", queryParameters), postData);
        }

        /// <summary>
        /// Runs a POST request against the specified complete path with query,
        /// using your OAuth credentials. Returns the raw response body
        /// returned by Factual.
        /// The necessary URL base will be automatically prepended to completePathWithQuery. If
        /// you need to change it, e.g. to make requests against a development instance of
        /// the Factual service, please set FactualApiUrlOverride property.
        /// Developer is entirly responsible for correct query formatting and URL encoding.
        /// </summary>
        /// <param name="completePathWithQuery">The complete path with query to run the request against.
        /// Example: "t/places-us/diffs?start=1354916463822&end=1354917903834"</param>
        /// <param name="postData">The POST content parameters to send with the request</param>
        /// <returns>The response body from running the POST request against Factual</returns>
        /// <exception cref="FactualApiException">If something goes wrong</exception>
        public string RequestPost(string completePathWithQuery, string postData)
        {
            var request = CreateWebRequest("POST", completePathWithQuery);
            if (Debug)
            {
                System.Diagnostics.Trace.WriteLine("==== Driver Version ====");
                System.Diagnostics.Trace.WriteLine(DriverHeaderTag);
                System.Diagnostics.Trace.WriteLine("==== Connection Timeout ====");
                System.Diagnostics.Trace.WriteLine(request.Timeout);
                System.Diagnostics.Trace.WriteLine("==== Read/Write Timeout ====");
                System.Diagnostics.Trace.WriteLine(request.ReadWriteTimeout);
                System.Diagnostics.Trace.WriteLine("\n\n==== Request Headers ====");
                System.Diagnostics.Trace.WriteLine(request.Headers);
                System.Diagnostics.Trace.WriteLine("==== Request Method ====");
                System.Diagnostics.Trace.WriteLine(request.Method);
                System.Diagnostics.Trace.WriteLine("==== Request Url ====");
                System.Diagnostics.Trace.WriteLine(request.RequestUri);
            }
            
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0 , byteArray.Length);
            }


            return ReadRequest(completePathWithQuery, request);
        }

        private string ReadRequest(string completePathWithQuery, HttpWebRequest request)
        {
            string jsonResult;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (Debug)
                    {
                        System.Diagnostics.Trace.WriteLine("\n\n==== Response Header ====");
                        System.Diagnostics.Trace.WriteLine(response.Headers);
                        System.Diagnostics.Trace.WriteLine("==== Response Status Code ====");
                        System.Diagnostics.Trace.WriteLine((int)response.StatusCode);
                        System.Diagnostics.Trace.WriteLine("==== Response Status Message ====");
                        System.Diagnostics.Trace.WriteLine(response.StatusDescription);
                    }

                    var stream = response.GetResponseStream();

                    if (stream == null)
                        throw new FactualException("Did not receive a response stream from factual");

                    using (var reader = new StreamReader(stream))
                    {
                        jsonResult = reader.ReadToEnd();
                        if (string.IsNullOrEmpty(jsonResult))
                            throw new FactualException("No data received from factual");

                        if (Debug)
                        {
                            System.Diagnostics.Trace.WriteLine("==== Response Type ====");
                            System.Diagnostics.Trace.WriteLine(response.ContentType);
                            System.Diagnostics.Trace.WriteLine("==== Response Body ====");
                            System.Diagnostics.Trace.WriteLine(jsonResult);
                            System.Diagnostics.Trace.WriteLine("\n================================================================================================================================================================\n\n");
                        }
                    }

                    if (response.StatusCode == (HttpStatusCode)301)
                    {
                        return RawQuery(FixUrlForRedirect(completePathWithQuery, jsonResult));
                    }
                }
            }
            catch (WebException ex)
            {
                var response = ((HttpWebResponse) ex.Response);

                if (response == null)
                {
                    throw;
                }

                if (Debug)
                {
                    System.Diagnostics.Trace.WriteLine("\n\n==== Response Header ====");
                    System.Diagnostics.Trace.WriteLine(response.Headers);
                    System.Diagnostics.Trace.WriteLine("==== Response Status Code ====");
                    System.Diagnostics.Trace.WriteLine((int)response.StatusCode);
                    System.Diagnostics.Trace.WriteLine("==== Response Status Message ====");
                    System.Diagnostics.Trace.WriteLine(response.StatusDescription);
                }

                var stream = response.GetResponseStream();

                if (stream == null)
                    throw new FactualApiException(response.StatusCode, string.Empty, completePathWithQuery);

                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();

                    if (Debug)
                    {
                        System.Diagnostics.Trace.WriteLine("==== Factual API Error ====");
                        System.Diagnostics.Trace.WriteLine(text);
                    }

                    throw new FactualApiException(response.StatusCode, text,
                                                  WebUtility.UrlDecode(completePathWithQuery));
                }
            }
            return jsonResult;
        }

        private string UrlForRaw(Dictionary<string, object> queryParameters)
        {
            string urlForRaw = "";
            foreach (var pair in queryParameters)
            {
                
                if (pair.Value.GetType().ToString().Contains("System.Collections.Generic.Dictionary"))
                    urlForRaw += WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(JsonConvert.SerializeObject(pair.Value)) + "&";
                else
                    urlForRaw += WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(pair.Value.ToString()) + "&";
            }
            if (urlForRaw.Length > 0)
				urlForRaw = urlForRaw.Remove(urlForRaw.Length - 1).Replace("%22%5b", "%5b").Replace("%5d%22", "%5d").Replace("=False", "=false").Replace("=True", "=true");
			return urlForRaw;
        }

        private string FixUrlForRedirect(string completePathWithQuery, string jsonResult)
        {
            dynamic json = JsonConvert.DeserializeObject(jsonResult);
            string oldId = (string) json.deprecated_id;
            string newId = (string) json.current_id;
            return completePathWithQuery.Replace(oldId, newId);
        }
    }
}
