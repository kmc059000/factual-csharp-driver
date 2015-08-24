﻿using System;
using System.Net;
using System.Net.Http;

namespace FactualDriver.OAuth
{
    /// <summary>
    /// Factual two legged oauth authenticator class
    /// oAuth signing. 
    /// </summary>
    public class OAuth2LeggedAuthenticator
    {
        private string _constumerKey;
        private string _constumerSecret;

        /// <summary>
        /// Authenticator constructor.
        /// </summary>
        /// <param name="consumerKey">oAuth consumer key.</param>
        /// <param name="consumerSecret">oAuth consumer secret key.</param>
        public OAuth2LeggedAuthenticator(string consumerKey, string consumerSecret)
        {
            _constumerKey = consumerKey;
            _constumerSecret = consumerSecret;
        }

        /// <summary>
        /// Adds authentication headers to the HttpWebRequest
        /// </summary>
        /// <param name="content"></param>
        /// <param name="requestUri"></param>
        /// <param name="method"></param>
        public void ApplyAuthenticationToRequest(HttpContent content, Uri requestUri, HttpMethod method)
        {
            string header = OAuthUtil.GenerateHeader(requestUri, _constumerKey, _constumerSecret, null, null, method.ToString());
            content.Headers.Add("Authorization", header);
        }
    }
}