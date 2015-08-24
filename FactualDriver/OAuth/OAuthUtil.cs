/* Copyright (c) 2011 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace FactualDriver.OAuth
{
    /// <summary>
    /// Provides a means to generate an OAuth signature.
    /// </summary>
    public class OAuthUtil
    {

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="httpMethod">The http method</param>
        /// <returns>The OAuth authorization header</returns>
        public static IDictionary<string, string> GenerateHeaders(Uri uri, string consumerKey, string consumerSecret, string httpMethod)
        {
            return GenerateHeaders(uri, consumerKey, consumerSecret, string.Empty, string.Empty, httpMethod);
        }

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="token">The OAuth token</param>
        /// <param name="tokenSecret">The OAuth token secret</param>
        /// <param name="httpMethod">The http method</param>
        /// <returns>The OAuth authorization header</returns>
        public static IDictionary<string, string> GenerateHeaders(Uri uri, string consumerKey, string consumerSecret, string token,
            string tokenSecret, string httpMethod)
        {
            OAuthParameters parameters = new OAuthParameters()
            {
                ConsumerKey = consumerKey,
                ConsumerSecret = consumerSecret,
                Token = token,
                TokenSecret = tokenSecret,
                SignatureMethod = OAuthBase.HMACSHA1SignatureType
            };
            return GenerateHeaders(uri, httpMethod, parameters);
        }

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="httpMethod">The http method</param>
        /// <param name="parameters">The OAuth parameters</param>
        /// <returns>The OAuth authorization header</returns>
        public static IDictionary<string, string> GenerateHeaders(Uri uri, string httpMethod, OAuthParameters parameters)
        {
            return GenerateHeadersInternal(uri, httpMethod, parameters).ToDictionary(x => x.Item1, x => x.Item2);
        }

        private static IEnumerable<Tuple<string, string>> GenerateHeadersInternal(Uri uri, string httpMethod, OAuthParameters parameters)
        {
            parameters.Timestamp = OAuthBase.GenerateTimeStamp();
            parameters.Nonce = OAuthBase.GenerateNonce();
            string signature = OAuthBase.GenerateSignature(uri, httpMethod, parameters);

            //sb.AppendFormat("Authorization: OAuth {0}=\"{1}\",", OAuthBase.OAuthVersionKey, OAuthBase.OAuthVersion);
            yield return Tuple.Create(OAuthBase.OAuthNonceKey, OAuthBase.EncodingPerRFC3986(parameters.Nonce));
            yield return Tuple.Create(OAuthBase.OAuthTimestampKey, OAuthBase.EncodingPerRFC3986(parameters.Timestamp));
            yield return Tuple.Create(OAuthBase.OAuthConsumerKeyKey, OAuthBase.EncodingPerRFC3986(parameters.ConsumerKey));
            

            if (parameters.BaseProperties.ContainsKey(OAuthBase.OAuthVerifierKey))
            {
                yield return Tuple.Create(OAuthBase.OAuthVerifierKey, 
                    OAuthBase.EncodingPerRFC3986(parameters.BaseProperties[OAuthBase.OAuthVerifierKey]));
            }
            if (!String.IsNullOrEmpty(parameters.Token))
            {
                yield return Tuple.Create(OAuthBase.OAuthTokenKey, OAuthBase.EncodingPerRFC3986(parameters.Token));
            }
            if (parameters.BaseProperties.ContainsKey(OAuthBase.OAuthCallbackKey))
            {
                yield return Tuple.Create(OAuthBase.OAuthCallbackKey, 
                    OAuthBase.EncodingPerRFC3986(parameters.BaseProperties[OAuthBase.OAuthCallbackKey]));
            }
            yield return Tuple.Create(OAuthBase.OAuthSignatureMethodKey, OAuthBase.HMACSHA1SignatureType);
            yield return Tuple.Create(OAuthBase.OAuthSignatureKey, OAuthBase.EncodingPerRFC3986(signature));
        }
    }
}
