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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ZubeNet.Common.Extensions;

namespace ZubeNet.Common.Rest.Concrete
{
    /// <inheritdoc />
    public sealed class ActionResultFactory : IActionResultFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        // private readonly ILogger _log;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ActionResultFactory" /> class.
        /// </summary>
        public ActionResultFactory(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper, ILogger log)
        {
            _httpContextAccessor = httpContextAccessor;
            //_log = new DecoratedLogger(log)
            //{
            //    Decorator = format => RequestLogFormatter.FormatMessage(HttpContext, format)
            //};

            UrlHelper = urlHelper;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        private IUrlHelper UrlHelper { get; }

        //
        // General
        //

        /// <inheritdoc />
        public IActionResult NonErrorResponse<T>(
            HttpStatusCode statusCode,
            T content,
            string contentType = "application/json",
            IDictionary<string, string> additionalHeaders = null)
        {
            var result = CreateNominalResult(statusCode, content, additionalHeaders);
            result.ContentTypes.Clear();
            result.ContentTypes.Add(contentType);
            return result;
        }

        //
        // 2xx
        //

        /// <inheritdoc />
        public IActionResult OkayResponse(IDictionary<string, string> additionalHeaders = null)
        {
            return OkayResponse<object>(null, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult OkayResponse<T>(T dto, IDictionary<string, string> additionalHeaders = null)
        {
            var result = CreateNominalResult(HttpStatusCode.OK, dto, additionalHeaders);
            result.ContentTypes.Add("application/json");
            return result;
        }

        /// <inheritdoc />
        public IActionResult CreatedResponse(string location, IDictionary<string, string> additionalHeaders = null)
        {
            return CreatedResponse<object>(location, null, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult CreatedResponse<T>(string location, T representation, IDictionary<string, string> additionalHeaders = null)
        {
            var copy = new Dictionary<string, string>(additionalHeaders ?? new Dictionary<string, string>())
            {
                ["Location"] = location
            };

            var result = CreateNominalResult(HttpStatusCode.Created, representation, copy);
            return result;
        }

        /// <inheritdoc />
        public IActionResult AcceptedResponse(IDictionary<string, string> additionalHeaders = null)
        {
            return AcceptedResponse<object>(null, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult AcceptedResponse<T>(T dto, IDictionary<string, string> additionalHeaders = null)
        {
            var result = CreateNominalResult(HttpStatusCode.Accepted, dto, additionalHeaders);
            result.ContentTypes.Add("application/json");
            return result;
        }

        /// <inheritdoc />
        public IActionResult NoContentResponse(IDictionary<string, string> additionalHeaders = null)
        {
            var result = CreateNominalResult(HttpStatusCode.NoContent, null, additionalHeaders);
            return result;
        }

        //
        // 3xx
        //

        /// <inheritdoc />
        public IActionResult SeeOtherResponse(string location, IDictionary<string, string> additionalHeaders = null)
        {
            var copy = new Dictionary<string, string>(additionalHeaders ?? new Dictionary<string, string>())
            {
                ["Location"] = location
            };

            var result = CreateNominalResult(HttpStatusCode.SeeOther, null, copy);
            return result;
        }

        /// <inheritdoc />
        public IActionResult NotModifiedResponse(IDictionary<string, string> additionalHeaders)
        {
            var result = CreateNominalResult(HttpStatusCode.NotModified, null, additionalHeaders);
            return result;
        }

        //
        // 4xx
        //

        /// <inheritdoc />
        public IActionResult BadRequestResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.BadRequest, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult ForbiddenResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.Forbidden, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult NotFoundResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.NotFound, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult ConflictResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.Conflict, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult PreconditionFailedResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.PreconditionFailed, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult RequestEntityTooLargeResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.RequestEntityTooLarge, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult PreconditionRequiredResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse((HttpStatusCode)428, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult UnauthorizedResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null)
        {
            return ErrorResponse(HttpStatusCode.Unauthorized, error, exception, additionalHeaders);
        }

        /// <inheritdoc />
        public IActionResult ErrorResponse(HttpStatusCode statusCode, ApiError error, Exception e = null, IDictionary<string, string> additionalHeaders = null)
        {
            var result = CreateErrorResult(statusCode, error, e, additionalHeaders);
            result.ContentTypes.Add("application/json");
            return result;
        }

        private ObjectResult CreateNominalResult(HttpStatusCode statusCode, object dto, IDictionary<string, string> additionalHeaders)
        {
            HttpContext.SetResponseStatusCode(statusCode);
            AddStandardHeaders();
            AddAdditionalHeaders(additionalHeaders);
            return new ObjectResult(dto);
        }

        private ObjectResult CreateNominalResult(HttpStatusCode statusCode, JObject dto, IDictionary<string, string> additionalHeaders)
        {
            HttpContext.SetResponseStatusCode(statusCode);
            AddStandardHeaders();
            AddAdditionalHeaders(additionalHeaders);
            return new ObjectResult(dto);
        }

        private ObjectResult CreateErrorResult(HttpStatusCode statusCode, object error, Exception exception, IDictionary<string, string> additionalHeaders)
        {
            Debug.Assert(error != null, "error != null");

            var exceptionMessage = exception != null ? $"Exception: {exception}\r\n" : null;
            var errorMessage = $"Error during request: {error}\r\n{exceptionMessage}";
            errorMessage = errorMessage.Replace("{", "{{").Replace("}", "}}");
            if (statusCode >= HttpStatusCode.InternalServerError)
            {
                // _log.LogError(0, exception, errorMessage);
            }

            HttpContext.SetResponseStatusCode(statusCode);
            AddStandardHeaders();
            AddAdditionalHeaders(additionalHeaders);

            var result = new ObjectResult(error);

            return result;
        }

        private void AddStandardHeaders()
        {
            HttpContext.Response.Headers[SemanticConstants.ServiceRequestIdHeaderName] = HttpContext.TraceIdentifier;
            HttpContext.Response.Headers[SemanticConstants.UserCorrelationIdHeader] = HttpContext.GetUserCorrelationId();
        }

        private void AddAdditionalHeaders(IDictionary<string, string> additionalHeaders)
        {
            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    HttpContext.Response.Headers[additionalHeader.Key] = additionalHeader.Value;
                }
            }
        }
    }
}