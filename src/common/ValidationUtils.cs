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

namespace ZubeNet.Common
{
    /// <summary>
    ///     Validation utilities to check method arguments.
    ///     Mostly wrapper methods that throw an exception if validation fails.
    /// </summary>
    /// <remarks>
    ///     Inspired by Newtonsoft.Json.Utilities.ValidationUtils and Ninject.Infrastructure.Ensure.
    /// </remarks>
    public static class ValidationUtils
    {
        public static void CheckArgumentNotNull(object paramValue, string paramName)
        {
            if (paramValue == null)
            {
                throw new ArgumentNullException(paramName, "Must not be null.");
            }
        }

        public static void CheckArgumentNotNullOrEmpty(string paramValue, string paramName)
        {
            if (string.IsNullOrEmpty(paramValue))
            {
                throw new ArgumentException("Must not be null or empty.", paramName);
            }
        }

        public static void CheckArgumentNotNullOrWhiteSpace(string paramValue, string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramValue))
            {
                throw new ArgumentException("Must not be null or empty.", paramName);
            }
        }

        public static void CheckGuidNotEmpty(Guid paramValue, string paramName)
        {
            if (paramValue == Guid.Empty)
            {
                throw new ArgumentException("Must not be empty", paramName);
            }
        }
    }
}