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
using System.Linq;

namespace ZubeNet.Common.Database
{
    public class ColumnInfo
    {
        public ColumnInfo(string columnName, Type clrDataType)
            : this(columnName, clrDataType, ConvertClrDataTypeToSql(clrDataType), IsClrDataTypeNullable(clrDataType))
        {
        }

        public ColumnInfo(string columnName, Type clrDataType, string sqlDataType, bool isNullable)
        {
            ColumnName = columnName;
            ClrDataType = clrDataType;
            SqlDataType = sqlDataType;
            IsNullable = isNullable;
            ShouldAddHashColumn = false;
        }

        public string ColumnName { get; }
        public Type ClrDataType { get; private set; }
        public string SqlDataType { get; }
        public bool IsNullable { get; }
        public bool ShouldAddHashColumn { get; set; }

        public IEnumerable<string> ToSqlColumnDefinitions()
        {
            var columnDefinitions = new List<string>
            {
                $" [{ColumnName}] {SqlDataType} {(IsNullable ? "NULL" : "NOT NULL")} "
            };

            if (ShouldAddHashColumn)
            {
                // A few notes on what's going on here:
                // * HASHBYTES by default returns VARBINARY(8000) which is way too big and isn't what we want to store.
                //   So we cast it to BINARY(16).
                // * We can get mixed-case strings which equate to the same thing (e.g. functions like "SizeTMult" vs "SIZETMult")
                //   but we want these to hash to the same thing, since we're not case sensitive about these values.
                //   So we convert all strings to UPPER to calculate the hash.
                // * We only want to calculate the hash _once_ (at row insertion) and so the column is PERSISTED.                
                columnDefinitions.Add($" [{ColumnName}Hash] AS CAST(dbo.udf_HashBytesLargeString('MD4', UPPER({ColumnName})) AS BINARY(16)) PERSISTED NOT NULL ");
            }

            return columnDefinitions.AsEnumerable();
        }

        public static string ConvertClrDataTypeToSql(Type clrDataType)
        {
            if (clrDataType == typeof(bool))
            {
                return "BIT";
            }
            if (clrDataType == typeof(byte))
            {
                return "TINYINT";
            }
            if (clrDataType == typeof(short))
            {
                return "SMALLINT";
            }
            if (clrDataType == typeof(int) || clrDataType == typeof(ushort))
            {
                return "INT";
            }
            if (clrDataType == typeof(long) || clrDataType == typeof(uint))
            {
                return "BIGINT";
            }
            if (clrDataType == typeof(double) || clrDataType == typeof(float))
            {
                return "FLOAT";
            }
            if (clrDataType == typeof(string))
            {
                return "NVARCHAR(MAX)";
            }
            if (clrDataType == typeof(DateTime))
            {
                return "DATETIME";
            }
            if (clrDataType == typeof(Guid))
            {
                return "UNIQUEIDENTIFIER";
            }
            throw new ArgumentException($"Do not know how to convert clr datatype {clrDataType} into Sql data type");
        }

        public static bool IsClrDataTypeNullable(Type clrDataType)
        {
            return !clrDataType.IsValueType || Nullable.GetUnderlyingType(clrDataType) != null;
        }
    }
}