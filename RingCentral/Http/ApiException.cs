using System;
using System.Net.Http;

namespace RingCentral.Http
{
    public class ApiException : Exception
    {
        public HttpResponseMessage Response { get; }
        public HttpRequestMessage Request { get; }

        public ApiException(string message) : base(message)
        {
        }

        public ApiException(string message, HttpResponseMessage response, HttpRequestMessage request) : base(message)
        {
            this.Response = response;
            this.Request = request;
        }
    }
}
