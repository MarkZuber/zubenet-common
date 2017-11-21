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

using System.Data.SqlClient;
using System.Linq;

namespace ZubeNet.Common.Database
{
    public static class SqlExtensions
    {
        public static bool IsUniqueKeyViolation(this SqlException e)
        {
            //
            // 2601 -> Cannot insert duplicate key row in object '%.*ls' with unique index '%.*ls'. The duplicate key value is %ls.
            // 2627 -> Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'. The duplicate key value is %ls.
            //
            // SELECT * FROM sys.messages
            //      WHERE text like '%duplicate%' and text like '%key%' and language_id = 1033
            //
            // https://www.microsoft.com/technet/support/ee/transform.aspx?ProdName=SQL%20Server&ProdVer=10.0&EvtID=2601&EvtSrc=MSSQLServer&LCID=1033
            // https://www.microsoft.com/technet/support/ee/transform.aspx?ProdName=SQL%20Server&ProdVer=10.0&EvtID=2627&EvtSrc=MSSQLServer&LCID=1033
            //
            return e.Errors.Cast<SqlError>().Any(error => error.Class == 14 && (error.Number == 2601 || error.Number == 2627));
        }

        public static bool IsDeadlock(this SqlException e)
        {
            return e.Number == SqlDatabase.SqlExceptionNumberDeadlock;
        }
    }
}