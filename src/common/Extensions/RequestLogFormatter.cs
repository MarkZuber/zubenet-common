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

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace ZubeNet.Common.Extensions
{
    public static class RequestLogFormatter
    {
        //public static FormatDecorator GetRequestDecorator(ILifetimeScope lifetimeScope)
        //{
        //    var accessor = new Lazy<IHttpContextAccessor>(() => lifetimeScope.Resolve<IHttpContextAccessor>());
        //    return s => FormatMessage(accessor.Value.HttpContext, s);
        //}

        public static string FormatMessage(HttpContext context, string message, params object[] args)
        {
            var formattedMessage = string.Format(CultureInfo.InvariantCulture, message, args);
            var footer = GenerateLogFooter(context);
            return $"{formattedMessage}\r\n{footer}";
        }

        private static string GenerateLogFooter(HttpContext context)
        {
            var requestId = context.TraceIdentifier;
            var clientCorrelationId = context.GetUserCorrelationId();
            return $"RequestUri: {context.GetFullRequestUri()}\r\nRequestId: {requestId}\r\nClientCorrelationId: {clientCorrelationId}";
        }
    }
}