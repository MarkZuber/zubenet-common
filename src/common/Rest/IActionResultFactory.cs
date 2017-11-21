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
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ZubeNet.Common.Rest
{
    /// <summary>
    ///     Factory for creating HTTP results.
    /// </summary>
    public interface IActionResultFactory
    {
        //
        // General
        //

        /// <summary>
        ///     Factory method to create a non-error response.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="content">The content, if any.</param>
        /// <param name="contentType">The content-type</param>
        /// <param name="additionalHeaders">Any additional header, if any.</param>
        /// <returns>The ActionResult.</returns>
        IActionResult NonErrorResponse<T>(HttpStatusCode statusCode, T content, string contentType = "application/json", IDictionary<string, string> additionalHeaders = null);

        //
        // 2xx
        //

        /// <summary>
        ///     Creates a 200 response.
        /// </summary>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 200 response.</returns>
        IActionResult OkayResponse(IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 200 response with the given content.
        /// </summary>
        /// <param name="dto">The content to include in the response.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 200 response.</returns>
        IActionResult OkayResponse<T>(T dto, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 201 response.
        /// </summary>
        /// <param name="location">The location of the created resource.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 200 response.</returns>
        IActionResult CreatedResponse(string location, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 201 response with the given content.
        /// </summary>
        /// <param name="location">The location of the created resource.</param>
        /// <param name="representation">The representation of the created resource.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 201 response.</returns>
        IActionResult CreatedResponse<T>(string location, T representation, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 202 response.
        /// </summary>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 202 response.</returns>
        IActionResult AcceptedResponse(IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 202 response with the given content.
        /// </summary>
        /// <param name="dto">The representation of the acccepted operation.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 202 response.</returns>
        IActionResult AcceptedResponse<T>(T dto, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 204 response.
        /// </summary>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 204 response.</returns>
        IActionResult NoContentResponse(IDictionary<string, string> additionalHeaders = null);

        //
        // 3xx
        //

        /// <summary>
        ///     Creates a 303 response.
        /// </summary>
        /// <param name="location">The location of the resource to which the client is being referred.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 303 response.</returns>
        IActionResult SeeOtherResponse(string location, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 304 response.
        /// </summary>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 304 response.</returns>
        IActionResult NotModifiedResponse(IDictionary<string, string> additionalHeaders = null);

        //
        // 4xx
        //

        /// <summary>
        ///     Creates a 400 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 400 response.</returns>
        IActionResult BadRequestResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 401 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 401 response.</returns>
        IActionResult UnauthorizedResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 403 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 403 response.</returns>
        IActionResult ForbiddenResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 404 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 404 response.</returns>
        IActionResult NotFoundResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 409 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 409 response.</returns>
        IActionResult ConflictResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 412 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 412 response.</returns>
        IActionResult PreconditionFailedResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 413 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 412 response.</returns>
        IActionResult RequestEntityTooLargeResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates a 428 response.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="exception">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in a 413 response.</returns>
        IActionResult PreconditionRequiredResponse(ApiError error, Exception exception = null, IDictionary<string, string> additionalHeaders = null);

        /// <summary>
        ///     Creates an error response.
        /// </summary>
        /// <param name="statusCode">The status code to give the response.</param>
        /// <param name="error">The error.</param>
        /// <param name="e">The exception that caused this error, if any.</param>
        /// <param name="additionalHeaders">The additional headers, if any.</param>
        /// <returns>An action result resulting in the given error response.</returns>
        IActionResult ErrorResponse(HttpStatusCode statusCode, ApiError error, Exception e = null, IDictionary<string, string> additionalHeaders = null);
    }
}