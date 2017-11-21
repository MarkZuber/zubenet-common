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
using System.Diagnostics;
using Newtonsoft.Json;

namespace ZubeNet.Common.Contracts
{
    /// <summary>
    ///     Contains all of the error codes for the API.
    ///     Adding a new error code is a breaking change.
    /// </summary>
    [JsonConverter(typeof(ServerErrorCodeConverter))]
    public sealed class ServerErrorCode : IEquatable<ServerErrorCode>
    {
        /// <summary>
        ///     The specified action is forbidden.
        /// </summary>
        public static readonly ServerErrorCode Forbidden = new ServerErrorCode(@"forbidden");

        /// <summary>
        ///     One or parameters is invalid.
        /// </summary>
        public static readonly ServerErrorCode InvalidArgument = new ServerErrorCode(@"invalid_argument");

        /// <summary>
        ///     A value was null.
        /// </summary>
        public static readonly ServerErrorCode NullValue = new ServerErrorCode(@"null_value");

        /// <summary>
        ///     The request resource cannot be found.
        /// </summary>
        public static readonly ServerErrorCode NotFound = new ServerErrorCode(@"not_found");

        /// <summary>
        ///     The input is too large.
        /// </summary>
        public static readonly ServerErrorCode Overflow = new ServerErrorCode(@"request_too_large");

        /// <summary>
        ///     An optimistic concurrency error has occurred.
        /// </summary>
        public static readonly ServerErrorCode ConcurrencyError = new ServerErrorCode(@"optimistic_concurrency_error");

        /// <summary>
        ///     An unexpected error occurred.
        /// </summary>
        public static readonly ServerErrorCode Unexpected = new ServerErrorCode(@"unexpected");

        /// <summary>
        ///     The user is not authorized to perform this action.
        /// </summary>
        public static readonly ServerErrorCode Unauthorized = new ServerErrorCode(@"unauthorized");

        /// <summary>
        ///     The value is already present.
        /// </summary>
        public static ServerErrorCode DuplicateValue = new ServerErrorCode(@"duplicate_value");

        /// <summary>
        ///     The operation is not valid at this time.
        /// </summary>
        public static ServerErrorCode InvalidOperation = new ServerErrorCode(@"invalid_operation");

        private readonly string _error;

        private ServerErrorCode(string error)
        {
            _error = error;
        }

        /// <inheritdoc />
        public bool Equals(ServerErrorCode errorCode)
        {
            return errorCode != null && _error.Equals(errorCode._error, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///     Casts the given error code to a string.
        /// </summary>
        /// <param name="errorCode">
        ///     The error code.
        /// </param>
        public static implicit operator string(ServerErrorCode errorCode)
        {
            Debug.Assert(errorCode != null, "errorCode != null");
            return errorCode._error;
        }

        /// <summary>
        ///     Determines if the two error codes are equal.
        /// </summary>
        /// <returns>true if equal; false otherwise.</returns>
        public static bool operator ==(ServerErrorCode left, ServerErrorCode right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        /// <summary>
        ///     Determines if the two error codes are not equal.
        /// </summary>
        /// <returns>true if not equal; false otherwise.</returns>
        public static bool operator !=(ServerErrorCode left, ServerErrorCode right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerErrorCode);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _error.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _error;
        }
    }

    /// <summary>
    ///     Serializes <see cref="ServerErrorCode" /> instances.
    /// </summary>
    public sealed class ServerErrorCodeConverter : JsonConverter
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var code = value as ServerErrorCode;
            writer.WriteToken(JsonToken.String, code?.ToString());
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ServerErrorCode);
        }
    }
}