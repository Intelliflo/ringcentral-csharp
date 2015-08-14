﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using RingCentral.Http;

namespace RingCentral
{
    public class Platform
    {
        private const string AccessTokenTtl = "3600"; // 60 minutes
        private const string RefreshTokenTtl = "36000"; // 10 hours
        private const string RefreshTokenTtlRemember = "604800"; // 1 week
        private const string TokenEndpoint = "/restapi/oauth/token";
        private const string RevokeEndpoint = "/restapi/oauth/revoke";
        private HttpClient _client;
        protected Auth Auth;

        public Platform(string appKey, string appSecret, string apiEndPoint)
        {
            AppKey = appKey;
            AppSecret = appSecret;
            ApiEndpoint = apiEndPoint;
            Auth = new Auth();
            _client = new HttpClient {BaseAddress = new Uri(ApiEndpoint)};
            _client.DefaultRequestHeaders.Add("SDK-Agent", "Ring Central C# SDK");
        }

        private string AppKey { get; set; }
        private string AppSecret { get; set; }
        private string ApiEndpoint { get; set; }

        /// <summary>
        ///     Method to generate Access Token and Refresh Token to establish an authenticated session
        /// </summary>
        /// <param name="userName">Login of RingCentral user</param>
        /// <param name="password">Password of the RingCentral User</param>
        /// <param name="extension">Optional: Extension number to login</param>
        /// <param name="isRemember">If set to true, refresh token TTL will be one week, otherwise it's 10 hours</param>
        /// <returns>string response of Authenticate result.</returns>
        public Response Authenticate(string userName, string password, string extension, bool isRemember)
        {
            var body = new Dictionary<string, string>
                       {
                           {"username", userName},
                           {"password", Uri.EscapeUriString(password)},
                           {"extension", extension},
                           {"grant_type", "password"},
                           {"access_token_ttl", AccessTokenTtl},
                           {"refresh_token_ttl", isRemember ? RefreshTokenTtlRemember : RefreshTokenTtl}
                       };

            var request = new Request(TokenEndpoint, body);
            var result = AuthPostRequest(request);

            Auth.SetRemember(isRemember);
            Auth.SetData(result.GetJson());

            return result;
        }

        /// <summary>
        ///     Refreshes expired Access token during valid lifetime of Refresh Token
        /// </summary>
        /// <returns>string response of Refresh result</returns>
        public Response Refresh()
        {
            if (!Auth.IsRefreshTokenValid()) throw new Exception("Refresh Token has Expired");

            var body = new Dictionary<string, string>
                       {
                           {"grant_type", "refresh_token"},
                           {"refresh_token", Auth.GetRefreshToken()},
                           {"access_token_ttl", AccessTokenTtl},
                           {"refresh_token_ttl", Auth.IsRemember() ? RefreshTokenTtlRemember : RefreshTokenTtl}
                       };

            var request = new Request(TokenEndpoint, body);
            var result = AuthPostRequest(request);

            Auth.SetData(result.GetJson());

            return result;
        }

        /// <summary>
        ///     Revokes the already granted access to stop application activity
        /// </summary>
        /// <returns>string response of Revoke result</returns>
        public Response Revoke()
        {
            var body = new Dictionary<string, string>
                       {
                           {"token", Auth.GetAccessToken()}
                       };

            Auth.Reset();

            var request = new Request(RevokeEndpoint, body);

            return AuthPostRequest(request);
        }

        /// <summary>
        ///     Authentication, Refresh and Revoke requests all require an Authentication Header Value of "Basic".  This is a
        ///     special method to handle those requests.
        /// </summary>
        /// <param name="request">
        ///     A Request object with a url and a dictionary of key value pairs (<c>Authenticate</c>,
        ///     <c>Refresh</c>, <c>Revoke</c>)
        /// </param>
        /// <returns>Response object</returns>
        public Response AuthPostRequest(Request request)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetApiKey());

            var result = _client.PostAsync(request.GetUrl(), request.GetHttpContent());

            return SetResponse(result);
        }

        /// <summary>
        ///     Gets the auth data set on authorization
        /// </summary>
        /// <returns>Dictionary of auth data</returns>

        public Dictionary<string, string> GetAuthData()
        {
            return Auth.GetAuthData();
        }


        /// <summary>
        ///     A HTTP POST request.
        ///     Http Content is set by using the proper constructor in the Request Object per endpoint needs
        /// </summary>
        /// <param name="request">A fully formed request object</param>
        /// <returns>A Response object</returns>
        public Response PostRequest(Request request)
        {
            CheckAccessAndOverRideHeaders(request);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Auth.GetAccessToken());

            var postResult = _client.PostAsync(request.GetUrl(), request.GetHttpContent());

            return SetResponse(postResult);
        }

        /// <summary>
        ///     A HTTP GET request.  Query parameters can be set via the appropriate constructor in the Request object.
        /// </summary>
        /// <param name="request">A fully formed request object</param>
        /// <returns>A Response object</returns>
        public Response GetRequest(Request request)
        {
            if (!IsAccessValid()) throw new Exception("Access has Expired");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Auth.GetAccessToken());

            var result = _client.GetAsync(request.GetUrl());

            return SetResponse(result);
        }

        /// <summary>
        ///     A HTTP DELETE request.
        /// </summary>
        /// <param name="request">A fully formed request object</param>
        /// <returns>A Response object</returns>
        public Response DeleteRequest(Request request)
        {
            if (!IsAccessValid()) throw new Exception("Access has Expired");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Auth.GetAccessToken());

            var deleteResult = _client.DeleteAsync(request.GetUrl());

            return SetResponse(deleteResult);
        }

        /// <summary>
        ///     A HTTP PUT request.
        ///     Http Content is set by using the proper constructor in the Request Object per endpoint needs
        /// </summary>
        /// <param name="request">A fully formed request object</param>
        /// <returns>A Response object</returns>
        public Response PutRequest(Request request)
        {
            if (!IsAccessValid()) throw new Exception("Access has Expired");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Auth.GetAccessToken());

            var putResult = _client.PutAsync(request.GetUrl(), request.GetHttpContent());

            return SetResponse(putResult);
        }

        /// <summary>
        ///     Creates a Response object based on the result of any of the HTTP methods called
        /// </summary>
        /// <param name="responseMessage">The passed in response message from the HTTP Methods</param>
        /// <returns>A Response object</returns>
        private Response SetResponse(Task<HttpResponseMessage> responseMessage)
        {
            var statusCode = Convert.ToInt32(responseMessage.Result.StatusCode);
            var body = responseMessage.Result.Content.ReadAsStringAsync().Result;
            var headers = responseMessage.Result.Content.Headers;

            ClearXhttpOverRideHeader();

            return new Response(statusCode, body, headers);
        }

        /// <summary>
        ///     Gets the API key by encoding the AppKey and AppSecret with Encoding.UTF8.GetBytes
        /// </summary>
        /// <returns>The Api Key</returns>
        private string GetApiKey()
        {
            var byteArray = Encoding.UTF8.GetBytes(AppKey + ":" + AppSecret);
            return Convert.ToBase64String(byteArray);
        }

        /// <summary>
        ///     Gets the HttpClient
        /// </summary>
        /// <returns>HttpClient</returns>
        public HttpClient GetClient()
        {
            return _client;
        }

        /// <summary>
        ///     Sets the HttpClient
        /// </summary>
        /// <param name="client">the Client to be set</param>
        public void SetClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        ///     Checks authorization Access and calls the SetXhttpOverRideHeader method
        /// </summary>
        /// <param name="request">A fully formed request object</param>
        private void CheckAccessAndOverRideHeaders(Request request)
        {
            if (!IsAccessValid()) throw new Exception("Access has Expired");

            SetXhttpOverRideHeader(request.GetXhttpOverRideHeader());
        }

        /// <summary>
        ///     Sets the X-HTTP-Method-Override to the method specified
        /// </summary>
        /// <param name="overrideMethod">The method that will override</param>
        private void SetXhttpOverRideHeader(string overrideMethod)
        {
            if (!string.IsNullOrEmpty(overrideMethod))
            {
                _client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", overrideMethod.ToUpper());
            }
        }

        /// <summary>
        ///     Removes the X-HTTP-Method-Override from the client
        /// </summary>
        private void ClearXhttpOverRideHeader()
        {
            if (_client.DefaultRequestHeaders.Contains("X-HTTP-Method-Override"))
            {
                _client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
            }
        }

        /// <summary>
        ///     Sets the user-agent header that will be passed with each request
        /// </summary>
        /// <param name="header">The value of the User-Agent header</param>
        public void SetUserAgentHeader(string header)
        {
            _client.DefaultRequestHeaders.Add("User-Agent", header);
        }

        /// <summary>
        ///     Determines if Access is valid and returns the boolean result.  If access is not valid but refresh token is valid
        ///     then a refresh is issued.
        /// </summary>
        /// <returns>boolean value of access authorization</returns>
        public bool IsAccessValid()
        {
            if (Auth.IsAccessTokenValid())
            {
                return true;
            }

            if (Auth.IsRefreshTokenValid())
            {
                Refresh();
                return true;
            }
            return false;
        }
    }
}