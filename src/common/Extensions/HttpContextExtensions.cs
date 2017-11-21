// MIT License
// 
// Copyright (c) 2017 Mark Zuber
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using ZubeNet.Common.Rest;

namespace ZubeNet.Common.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetUserCorrelationId(this HttpContext context)
        {
            StringValues clientCorrelationIdHeaderValues;
            string clientCorrelationId = null;
            if (context.Request.Headers.TryGetValue(SemanticConstants.UserCorrelationIdHeader, out clientCorrelationIdHeaderValues))
            {
                clientCorrelationId = clientCorrelationIdHeaderValues[0];
            }

            return clientCorrelationId;
        }

        public static void SetResponseToForbidden(this HttpContext context)
        {
            context.SetResponseStatusCode(HttpStatusCode.Forbidden);
        }

        public static void SetResponseToBadRequest(this HttpContext context)
        {
            context.SetResponseStatusCode(HttpStatusCode.BadRequest);
        }

        public static void SetResponseToNotFound(this HttpContext context)
        {
            context.SetResponseStatusCode(HttpStatusCode.NotFound);
        }

        public static void SetResponseStatusCode(this HttpContext context, HttpStatusCode statusCode)
        {
            context.Response.StatusCode = (int)statusCode;
        }

        public static Uri GetFullRequestUri(this HttpContext context)
        {
            var queryString = context.Request.QueryString;
            var absoluteUri = string.Concat(
                context.Request.Scheme,
                "://",
                context.Request.Host.ToUriComponent(),
                context.Request.PathBase.ToUriComponent(),
                context.Request.Path.ToUriComponent(),
                queryString.ToUriComponent());
            return new Uri(absoluteUri);
        }

        public static string GetApiVersion(this HttpContext self)
        {
            object apiVersionObject;
            if (!self.Items.TryGetValue("apiVersion", out apiVersionObject))
            {
                var path = self.Request.Path.Value;
                var apiIndex = path.IndexOf(SemanticConstants.RouteBasePathApiPrefix, StringComparison.OrdinalIgnoreCase);
                if (apiIndex == -1)
                {
                    self.Items["apiVersion"] = null;
                }
                else
                {
                    var secondSlashIndex = path.IndexOf("/", apiIndex + SemanticConstants.RouteBasePathApiPrefix.Length, StringComparison.OrdinalIgnoreCase);
                    if (secondSlashIndex == -1)
                    {
                        var apiVersion = path.Substring(apiIndex + SemanticConstants.RouteBasePathApiPrefix.Length);
                        self.Items["apiVersion"] = apiVersion;
                        apiVersionObject = apiVersion;
                    }
                    else
                    {
                        var count = secondSlashIndex - apiIndex - SemanticConstants.RouteBasePathApiPrefix.Length;
                        var apiVersion = path.Substring(apiIndex + SemanticConstants.RouteBasePathApiPrefix.Length, count);
                        self.Items["apiVersion"] = apiVersion;
                        apiVersionObject = apiVersion;
                    }
                }
            }

            return (string)apiVersionObject;
        }
    }
}