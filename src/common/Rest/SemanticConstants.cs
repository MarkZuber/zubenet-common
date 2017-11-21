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

namespace ZubeNet.Common.Rest
{
    public static class SemanticConstants
    {
        //
        // routing constants.
        //
        // /api/{apiVersion}
        public const string ApiVersionRouteValueName = "apiVersion";

        public const string RouteBasePathApiPrefix = "/api/";
        public const string RouteBasePath = RouteBasePathApiPrefix + "{" + ApiVersionRouteValueName + "}";

        //
        // Header constants
        //
        public const string ServiceRequestIdHeaderName = "x-ms-request-id";

        public const string UserCorrelationIdHeader = "x-ms-user-correlation-id";

        //
        // these strings map to semantics.json, and are included for ease of use throughtout
        // the code.
        //
        public const string NotFound = "notFound";

        public const string BadRequest = "badRequest";
        public const string NotImplemented = "notImplemented";
        public const string Unknown = "unknown";
        public const string PreconditionFailed = "preconditionFailed";
        public const string Unauthorized = "unauthorized";
        public const string Forbidden = "forbidden";
    }
}