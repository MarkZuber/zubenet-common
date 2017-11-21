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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace ZubeNet.Common
{
    [Serializable]
    public class SqlConnectionException : Exception
    {
        private static readonly Regex PasswordFindRegex = new Regex("(Password[\\s]*=[\\s]*)([^\\s;]+)");
        private string _sanitizedConnectionString;

        public SqlConnectionException()
        {
        }

        public SqlConnectionException(string message)
            : base(message)
        {
        }

        public SqlConnectionException(string message, string connectionString)
            : base(message)
        {
            ConnectionString = connectionString;
        }

        public SqlConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SqlConnectionException(string message, string connectionString, Exception innerException)
            : base(message, innerException)
        {
            ConnectionString = connectionString;
        }

        protected SqlConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private string ConnectionString
        {
            get => _sanitizedConnectionString;
            set => _sanitizedConnectionString = SanitizeConnectionString(value);
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("ConnectionString", ConnectionString);
            base.GetObjectData(info, context);
        }

        /// <summary>
        ///     Super simple sanitizer. Does not attempt to handle quoted string with space, etc.
        /// </summary>
        public static string SanitizeConnectionString(string connectionString)
        {
            // Remove password.
            return PasswordFindRegex.Replace(connectionString, "$1******");
        }
    }
}