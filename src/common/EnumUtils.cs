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
using System.ComponentModel;
using System.Linq;

namespace ZubeNet.Common
{
    /// <summary>
    ///     Enum utilities
    /// </summary>
    public static class EnumUtils
    {
        /// <summary>
        ///     Gets the Description attribute for an Enum
        /// </summary>
        /// <param name="value">Enum to get the description of</param>
        /// <returns>The Description attribute of the Enum, or the Enum's name if the attribute doesn't exist</returns>
        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            // return the string representation (enum name) if Description was not found
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        /// <summary>
        ///     Parses an enum from a description
        /// </summary>
        /// <param name="description">Enum Description attribute</param>
        /// <returns>Enum whose Description is specified</returns>
        /// <typeparam name="T">Type of Enum</typeparam>
        public static T ParseEnumDescription<T>(string description)
        {
            var enumType = typeof(T);

            if (enumType.BaseType != typeof(Enum))
            {
                throw new InvalidEnumArgumentException("T must be of type System.Enum");
            }

            var enumValueList = Enum.GetValues(enumType);

            foreach (T enumSearch in enumValueList)
            {
                if ((enumSearch as Enum).GetDescription() == description)
                {
                    return enumSearch;
                }
            }

            throw new InvalidEnumArgumentException("Enum with specified description not found");
        }

        /// <summary>
        ///     Extension method to GetEnumDescription
        /// </summary>
        /// <param name="value">Value of enum</param>
        /// <returns>The Description attribute of the Enum, or the Enum's name if the attribute doesn't exist</returns>
        public static string GetDescription(this Enum value)
        {
            return GetEnumDescription(value);
        }

        public static bool IsDefined(this Enum value)
        {
            return Enum.IsDefined(value.GetType(), value);
        }

        public static bool IsValidEnumValue(this Enum value)
        {
            return value.HasFlags() ? IsFlagsEnumDefined(value) : value.IsDefined();
        }

        private static bool IsFlagsEnumDefined(Enum value)
        {
            // modeled after Enum's InternalFlagsFormat
            var underlyingenumtype = Enum.GetUnderlyingType(value.GetType());
            switch (Type.GetTypeCode(underlyingenumtype))
            {
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            {
                Activator.CreateInstance(underlyingenumtype);
                var svalue = Convert.ToInt64(value);
                if (svalue < 0)
                {
                    throw new ArgumentException($"Can't process negative {svalue} as {value.GetType().Name} enum with flags");
                }
            }
                break;
            }

            var flagsset = Convert.ToUInt64(value);
            var values = Enum.GetValues(value.GetType()); // .Cast<ulong />().ToArray<ulong />();
            var flagno = values.Length - 1;
            var initialflags = flagsset;
            ulong flag = 0;

            // start with the highest values
            while (flagno >= 0)
            {
                flag = Convert.ToUInt64(values.GetValue(flagno));
                if ((flagno == 0) && (flag == 0))
                {
                    break;
                }
                // if the flags set contain this flag
                if ((flagsset & flag) == flag)
                {
                    // unset this flag
                    flagsset -= flag;
                    if (flagsset == 0)
                    {
                        return true;
                    }
                }
                flagno--;
            }
            if (flagsset != 0)
            {
                return false;
            }
            return initialflags != 0 || flag == 0;
        }

        public static bool HasFlags(this Enum value)
        {
            return value.GetType().GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
        }

        /// <summary>
        ///     Gets the attributes of type T for an Enum.
        /// </summary>
        /// <param name="value">Enum to get the description of</param>
        /// <returns>The array of attributes of type T.</returns>
        public static T[] GetEnumAttributes<T>(Enum value)
            where T : Attribute
        {
            var fi = value.GetType().GetField(value.ToString());
            return fi.GetCustomAttributes(typeof(T), false).OfType<T>().ToArray();
        }

        /// <summary>
        ///     Gets the first attribute of type T for an Enum (if any).
        /// </summary>
        /// <param name="value">Enum to get the description of</param>
        /// <returns>The first attribute of type T, if any, otherwise null.</returns>
        public static T GetEnumAttribute<T>(Enum value)
            where T : Attribute
        {
            var attributes = GetEnumAttributes<T>(value);
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0];
            }

            return null;
        }

        /// <summary>
        ///     Parses the given enum value.
        /// </summary>
        /// <typeparam name="T">
        ///     The enum type.
        /// </typeparam>
        /// <param name="value">
        ///     The value to parse.
        /// </param>
        /// <returns>
        ///     The parsed enum value.
        /// </returns>
        public static T Parse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, false);
        }

        /// <summary>
        ///     Parses the given enum value.
        /// </summary>
        /// <typeparam name="T">
        ///     The enum type.
        /// </typeparam>
        /// <param name="value">
        ///     The value to parse.
        /// </param>
        /// <param name="ignoreCase">
        ///     true to ignore casing when parsing; false to match exact case.
        /// </param>
        /// <returns>
        ///     The parsed enum value.
        /// </returns>
        public static T Parse<T>(string value, bool ignoreCase)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }
    }
}