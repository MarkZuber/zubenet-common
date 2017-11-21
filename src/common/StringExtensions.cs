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
using System.Text;
using System.Text.RegularExpressions;

namespace ZubeNet.Common
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Regular expression pattern used to match representations of a number of bytes.
        /// </summary>
        private const string NumberOfBytesPatten = @"^([0-9]+)\s*(KiB|MiB|GiB)?$";

        /// <summary>
        ///     Whitespace characters used by <see cref="ContainsWhitespace(string)" />.
        /// </summary>
        private static readonly char[] WhitespaceCharacters =
        {
            ' ',
            '\t',
            '\r',
            '\n',
        };

        private static string EncloseWithString(this string val, char leftCharToEnclose, char rightCharToEnclose, bool forceEnclose)
        {
            string newVal = null;
            if (val != null)
            {
                newVal = val.Trim();
            }

            if (string.IsNullOrEmpty(newVal))
            {
                return $"{leftCharToEnclose}{rightCharToEnclose}";
            }

            if (forceEnclose)
            {
                return $"{leftCharToEnclose}{newVal}{rightCharToEnclose}";
            }
            if (newVal[0] == leftCharToEnclose)
            {
                if (newVal[newVal.Length - 1] != rightCharToEnclose)
                {
                    throw new ArgumentException($"String already starts with a {leftCharToEnclose} but doesn't end with {rightCharToEnclose}.");
                }

                // String starts and ends with charToEnclose
                return newVal;
            }
            if (newVal[newVal.Length - 1] == rightCharToEnclose)
            {
                throw new ArgumentException($"String doesn't start with a {leftCharToEnclose} but already ends with {rightCharToEnclose}.");
            }

            return $"{leftCharToEnclose}{newVal}{rightCharToEnclose}";
        }

        private static string EncloseWithString(this string val, char charToEnclose, bool forceEnclose)
        {
            return EncloseWithString(val, charToEnclose, charToEnclose, forceEnclose);
        }

        /// <summary>
        ///     Extension method on string that encloses the string in double quotes, if it's not already enclosed in them.
        ///     If the string already starts with " but doesn't end with them (or vice versa) an ArgumentException is thrown.
        /// </summary>
        /// <param name="val">The string to be enclosed in quotes.</param>
        /// <returns>The string enclosed in quotes.</returns>
        public static string EncloseQuotes(this string val)
        {
            return val.EncloseWithString('\"', false);
        }

        public static string EncloseQuotes(this string val, bool forceEnclose)
        {
            return val.EncloseWithString('\"', forceEnclose);
        }

        public static string EncloseBrackets(this string val)
        {
            return val.EncloseWithString('[', ']', false);
        }

        public static string EncloseBrackets(this string val, bool forceEnclose)
        {
            return val.EncloseWithString('[', ']', forceEnclose);
        }

        /// <summary>
        ///     Extension method on string that encloses the string in single quotes, if it's not already enclosed in them.
        ///     If the string already starts with ' but doesn't end with it (or vice versa) an ArgumentException is thrown.
        /// </summary>
        /// <param name="input">The string to be enclosed in single quotes.</param>
        /// <returns>The string enclosed in single quotes.</returns>
        public static string EncloseSingleQuotes(this string input)
        {
            return input.EncloseWithString('\'', false);
        }

        public static string EncloseSingleQuotes(this string input, bool forceEnclose)
        {
            return input.EncloseWithString('\'', forceEnclose);
        }

        /// <summary>
        ///     Trims whitespace from the input string and if it starts AND ends with " then it returns the enclosed string without
        ///     the " characters.
        ///     Otherwise, returns the input string.
        ///     So sending in '  "Foo" '  will return 'Foo'.
        ///     sending in '  "Foo '   will return '  "Foo '.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveEnclosingQuotes(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var newVal = input.Trim();
            if (newVal.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && newVal.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                return newVal.Substring(1, newVal.Length - 2);
            }

            return input;
        }

        /// <summary>
        ///     Returns a null string if the string is either null or empty
        ///     <para />
        ///     Useful for ?? operator (http://msdn.microsoft.com/en-us/library/ms173224.aspx)
        /// </summary>
        /// <example>string displayText = Request.Form["name"] ?? "unknown";</example>
        /// <param name="value">Input string to parse</param>
        /// <returns>null if string is null or String.Empty.  String's value otherwise</returns>
        public static string ToNullIfEmpty(this string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static string CutoffWithElipses(this string value, int length)
        {
            return value.Length > length ? $"{value.Substring(0, length)}..." : value;
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static byte[] FromBase64ToBytes(this string s)
        {
            return Convert.FromBase64String(s);
        }

        public static string ToBase64String(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static string ToBase64(this int value)
        {
            // Convert 4-byte Int32 to Byte[4] and Base-64 encode it
            return Convert.ToBase64String(BitConverter.GetBytes(value)); // Byte[<=64]
        }

        public static int FromBase64ToInt32(this string value)
        {
            return BitConverter.ToInt32(Convert.FromBase64String(value), 0);
        }

        public static byte[] Encode(this string s, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetBytes(s);
        }

        public static string Decode(this byte[] data, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetString(data);
        }

        public static string Decode(this byte[] data, int index, int count, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetString(data, index, count);
        }

        public static bool StartsWithInsensitive(this string @string, string prefix)
        {
            return string.Compare(@string, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string ToCsvLine(this IEnumerable<string> elements)
        {
            // This methods purposely does NOT accept Objects to avoid unnecessary boxings.
            // This forces callers to call ToString thereby reducing memory overhead and improving performance.
            var sb = new StringBuilder();
            foreach (var e in elements)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                var isString = e != null;
                if (isString)
                {
                    sb.Append('\"');
                }
                sb.Append(e);
                if (isString)
                {
                    sb.Append('\"');
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        ///     Converts an arracy of Strings to a CSV line terminted with a carriage-return/linefeed.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static string ToCsvLine(params string[] elements)
        {
            // This methods purposely does NOT accept Objects to avoid unnecessary boxings.
            // This forces callers to call ToString thereby reducing memory overhead and improving performance.
            return ToCsvLine((IEnumerable<string>)elements);
        }

        /// <summary>
        ///     Determines if a string contains any whitespace characters.
        /// </summary>
        /// <param name="target">Target string</param>
        /// <returns>True if string contains whitespace</returns>
        public static bool ContainsWhitespace(this string target)
        {
            return target.IndexOfAny(WhitespaceCharacters) >= 0;
        }

        /// <summary>
        ///     Parse a string which contains a number of bytes, such as "50 GiB".
        /// </summary>
        /// <param name="self">String to parse.</param>
        /// <returns>Number of bytes.</returns>
        public static long ParseNumberOfBytes(this string self)
        {
            var pattern = new Regex(NumberOfBytesPatten);

            var match = pattern.Match(self);

            if (!match.Success || match.Groups.Count < 2 || match.Groups.Count > 3)
            {
                throw new FormatException($"The string '{self}' could not be parsed into a number of bytes.");
            }

            long ret = int.Parse(match.Groups[1].ToString());

            if (match.Groups.Count > 2)
            {
                var suffix = match.Groups[2].ToString();
                switch (suffix)
                {
                case "KiB":
                    ret <<= 10;
                    break;
                case "MiB":
                    ret <<= 20;
                    break;
                case "GiB":
                    ret <<= 30;
                    break;
                case "":
                    // No suffix at all
                    break;
                default: throw new FormatException($"Unknown size suffix '{suffix}'.");
                }
            }

            return ret;
        }

        /// <summary>
        ///     Converts a wildcard pattern to a regular expression pattern.
        /// </summary>
        /// <param name="self">Wildcard pattern to convert.</param>
        /// <returns>Regular expression pattern.</returns>
        public static string WildcardPatternToRegex(this string self)
        {
            return $"^{Regex.Escape(self).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
        }
    }
}