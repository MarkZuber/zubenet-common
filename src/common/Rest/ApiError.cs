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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ZubeNet.Common.Rest
{
    public class ApiError
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ApiError" /> class.
        /// </summary>
        /// <param name="code">
        ///     A server defined error code.
        /// </param>
        /// <param name="message">
        ///     A message describing the error.
        /// </param>
        /// <param name="target">
        ///     The target of the error.
        /// </param>
        public ApiError(string code, string message, string target)
            : this(code, message, target, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ApiError" /> class.
        /// </summary>
        /// <param name="code">
        ///     A server defined error code.
        /// </param>
        /// <param name="message">
        ///     A message describing the error.
        /// </param>
        /// <param name="target">
        ///     The target of the error.
        /// </param>
        /// <param name="details">
        ///     Any additional details about the error.
        ///     This paramter may be <c>null</c>.
        /// </param>
        public ApiError(string code, string message, string target, IEnumerable<ApiError> details)
            : this(code, message, target, details, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ApiError" /> class.
        /// </summary>
        /// <param name="code">
        ///     A server defined error code.
        /// </param>
        /// <param name="message">
        ///     A message describing the error.
        /// </param>
        /// <param name="target">
        ///     The target of the error.
        /// </param>
        /// <param name="details">
        ///     Any additional details about the error.
        ///     This paramter may be <c>null</c>.
        /// </param>
        /// <param name="innerError">
        ///     An additional object that may contain more specific information about the error.
        ///     This paramter may be <c>null</c>.
        /// </param>
        [JsonConstructor]
        public ApiError(string code, string message, string target, IEnumerable<ApiError> details, object innerError)
        {
            Code = code;
            Message = message;
            Target = target;
            Details = details ?? new List<ApiError>();
            InnerError = innerError;
        }

        /// <summary>
        ///     Gets a server-defined error code.
        /// </summary>
        [Required]
        [JsonProperty("code")]
        public string Code { get; }

        /// <summary>
        ///     Gets a human-readable representation of the error.
        /// </summary>
        [Required]
        [JsonProperty("message")]
        public string Message { get; }

        /// <summary>
        ///     Gets the target of the error.
        /// </summary>
        [Required]
        [JsonProperty("target")]
        public string Target { get; }

        /// <summary>
        ///     Gets an array of details about specific errors that led to this reported error.
        /// </summary>
        [JsonProperty("details")]
        public IEnumerable<ApiError> Details { get; }

        /// <summary>
        ///     Gets an object containing more specific information than the current object about the error.
        /// </summary>
        [JsonProperty("innererror")]
        public object InnerError { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, CoreConstants.ServiceSerializerSettings);
        }
    }
}