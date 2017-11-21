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
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace ZubeNet.Common.Database
{
    public interface ISqlDatabase : IDisposable
    {
        IDbConnection DbConnection { get; }
        SqlConnection SqlConnection { get; }
        TextWriter Log { get; set; }
        int SqlTimeout { get; set; }
        long GetTableSizeFromSpaceUsedSproc(string tableName);

        SqlCommand PrepareSqlCommand(string sqlQuery, CommandType commandType, ICollection<SqlParameter> inputParameters);

        SqlCommand PrepareSqlCommand(string sqlQuery, CommandType commandType, ICollection<SqlParameter> inputParameters, int sqlTimeout);

        SqlCommand PrepareSqlCommand(
            string sqlQuery,
            CommandType commandType,
            ICollection<SqlParameter> inputParameters,
            Dictionary<string, SqlParameter> outputParameters,
            int sqlTimeout);

        T GetRowByPrimaryKeyId<T>(int primaryKeyId, string tableName, string primaryKeyFieldName)
            where T : class, new();

        void ExecuteNonQuery(string query);
        void ExecuteNonQuery(string query, ManualResetEvent cancelRequested);
        void ExecuteNonQuery(string query, Collection<SqlParameter> inputParameters);
        void ExecuteNonQuery(string query, Collection<SqlParameter> inputParameters, ManualResetEvent cancelRequested);

        /// <summary>
        ///     Runs a stored procedure that only takes input parameters.
        /// </summary>
        /// <param name="storedProc">The name of the stored procedure.</param>
        /// <param name="inputParameters">A list of SqlParameter objects representing the input parameters to the stored procedure.</param>
        void ExecuteStoredProc(string storedProc, Collection<SqlParameter> inputParameters);

        /// <summary>
        ///     Runs a stored proceedure that takes input parameters and returns output parameters.
        ///     The output parameters are in a Dictionary using a helpful name as the key and the SqlParameter as the value to
        ///     allow
        ///     simple retrieval of the output parameters after calling RunStoredProc().
        /// </summary>
        /// <param name="storedProc">The name of the stored procedure.</param>
        /// <param name="inputParameters">A list of SqlParameter objects representing the input parameters to the stored procedure.</param>
        /// <param name="outputParameters">Dictionary mapping names to output parameters so they can be found easily later.</param>
        void ExecuteStoredProc(string storedProc, Collection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters);

        void ExecuteStoredProc(string storedProc, Collection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters, ManualResetEvent cancelRequested);

        /// <summary>
        ///     Executes a query that will return a list of 0 or more objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        IEnumerable<T> ExecuteQueryToObjects<T>(string sqlQuery);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        void ExecuteQueryToObjects<T1, T2>(string sqlQuery, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1, T2, and T3, respectively, from
        ///     three result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <typeparam name="T3">The type of object(s) to return from the third result set.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        /// <param name="resultSet3">Receives an IEnumerable of the object(s) found in the third result set.</param>
        void ExecuteQueryToObjects<T1, T2, T3>(string sqlQuery, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2, out IEnumerable<T3> resultSet3);

        /// <summary>
        ///     Executes a query that will return a list of 0 or more objects of type T.  This function takes a list of
        ///     SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        IEnumerable<T> ExecuteQueryToObjects<T>(string sqlQuery, ICollection<SqlParameter> parameters);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        ///     This function takes a list of SqlParameters as input for the stored procedure.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        void ExecuteQueryToObjects<T1, T2>(string sqlQuery, ICollection<SqlParameter> parameters, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1, T2, and T3, respectively, from
        ///     three result sets.
        ///     This function takes a list of SqlParameters as input for the stored procedure.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <typeparam name="T3">The type of object(s) to return from the third result set.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        /// <param name="resultSet3">Receives an IEnumerable of the object(s) found in the third result set.</param>
        void ExecuteQueryToObjects<T1, T2, T3>(
            string sqlQuery,
            ICollection<SqlParameter> parameters,
            out IEnumerable<T1> resultSet1,
            out IEnumerable<T2> resultSet2,
            out IEnumerable<T3> resultSet3);

        /// <summary>
        ///     Executes a query that will return a single object of type T (or default(T)) if nothing is found.
        ///     This function takes a list of SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An object of type T or default(T) if nothing is found.</returns>
        T ExecuteQueryToObject<T>(string sqlQuery, ICollection<SqlParameter> parameters);

        /// <summary>
        ///     Executes a stored proc that will return a list of 0 or more objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="storedProc">The name of the stored proc to run</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to run.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        void ExecuteStoredProcToObjects<T1, T2>(string storedProc, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1, T2, and T3, respectively, from
        ///     three result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <typeparam name="T3">The type of object(s) to return from the third result set.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to run.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        /// <param name="resultSet3">Receives an IEnumerable of the object(s) found in the third result set.</param>
        void ExecuteStoredProcToObjects<T1, T2, T3>(string storedProc, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2, out IEnumerable<T3> resultSet3);

        /// <summary>
        ///     Executes a stored proc that will return a list of 0 or more objects of type T.  This function takes a list of
        ///     SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="storedProc">The name of the stored proc to run</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc, ICollection<SqlParameter> parameters);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        ///     This function takes a list of SqlParameters as input for the stored procedure.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to run.</param>
        /// <param name="parameters">List of SqlParameters that are passed to the stored procedure.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        void ExecuteStoredProcToObjects<T1, T2>(string storedProc, ICollection<SqlParameter> parameters, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2);

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1, T2, and T3, respectively, from
        ///     three result sets.
        ///     This function takes a list of SqlParameters as input for the stored procedure.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <typeparam name="T3">The type of object(s) to return from the third result set.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to run.</param>
        /// <param name="parameters">List of SqlParameters that are passed to the stored procedure.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        /// <param name="resultSet3">Receives an IEnumerable of the object(s) found in the third result set.</param>
        void ExecuteStoredProcToObjects<T1, T2, T3>(
            string storedProc,
            ICollection<SqlParameter> parameters,
            out IEnumerable<T1> resultSet1,
            out IEnumerable<T2> resultSet2,
            out IEnumerable<T3> resultSet3);

        /// <summary>
        ///     Executes a stored proc that will return a single object of type T (or default(T)) if nothing is found.
        ///     This function takes a list of SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="storedProc">The stored proc.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An object of type T or default(T) if nothing is found.</returns>
        T ExecuteStoredProcToObject<T>(string storedProc, ICollection<SqlParameter> parameters);

        IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc, Collection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters);

        DataSet ExecuteQueryToDataSet(string sqlQuery, ICollection<SqlParameter> parameters);

        DataSet ExecuteStoredProcToDataSet(string storedProc, ICollection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters);

        SqlConnectionStats GetConnectionStats();

        /// <summary>
        ///     Executes the query, and returns the first column of the first row in the result set returned by the query.
        ///     Additional columns or rows are ignored.
        /// </summary>
        /// <param name="sqlQuery">The query to execute, such as "SELECT COUNT(*) FROM..."</param>
        /// <param name="parameters">A list of SqlParameters to use with the query.</param>
        /// <returns>The first column of the first row in the result set, or a null reference if the result set is empty.</returns>
        object ExecuteQueryToScalar(string sqlQuery, ICollection<SqlParameter> parameters);

        DataTable CreateDataTable(string tableName, IEnumerable<ColumnInfo> columnInfos);

        string CreateTempTableFromColumnInfos(string tableName, string tableUniquenessKey, IEnumerable<ColumnInfo> columnInfos);

        void CreateAndInsertTempTable(string tableName, DataTable dataTable, bool tableLock);
        void BulkInsertDataTable(string tableName, DataTable dataTable, bool tableLock, bool identityInsert);
        void BulkInsertDataTable(string tableName, DataTable dataTable, bool tableLock);
        void BulkInsertDataTable(string tableName, DataTable dataTable);

        void BulkInsertDataTable(string tableName, string tableUniquenessKey, DataTable dataTable, IEnumerable<ColumnInfo> columnInfos);

        void BulkInsertDataTable(string tableName, IDataReader dataReader, bool tableLock);
        void BulkInsertDataTable(string tableName, IDataReader dataReader);
        void BulkInsertDataTable(string tableName, IDataReader dataReader, bool tableLock, bool identityInsert);
        void DropTable(string tableName);
        bool CheckParameterExists(string sprocName, string parameterName);

        void MergeExplicitStringsIntoLookupTable(IEnumerable<string> explicitStrings, string targetTableName, string targetColumnName, ManualResetEvent cancelRequestedEvent);

        void MergeStringsIntoLookupTableIgnoreNulls(
            string sourceTableName,
            string sourceColumnName,
            string targetTableName,
            string targetColumnName,
            ManualResetEvent cancelRequestedEvent);

        void MergeStringsIntoLookupTable(string sourceTableName, string sourceColumnName, string targetTableName, string targetColumnName, ManualResetEvent cancelRequestedEvent);

        void MergeStringsIntoLookupTableWithHash(
            string sourceTableName,
            string sourceColumnName,
            string targetTableName,
            string targetColumnName,
            string targetHashColumnName,
            ManualResetEvent cancelRequestedEvent);

        void MergeStringsWithMultiColumnsIntoLookupTableWithHash(
            string sourceTableName,
            string sourceStringColumnName,
            IEnumerable<string> sourceOtherColumnNames,
            string targetTableName,
            string targetStringColumnName,
            IEnumerable<string> targetOtherColumnNames,
            string targetHashColumnName,
            ManualResetEvent cancelRequestedEvent);

        void BulkInsertAndMergeStringList(
            string baseTableName,
            string tableNameUniquenessDifferentiator,
            IEnumerable<string> stringsToMerge,
            string sourceColumnType,
            string targetTableName,
            string targetColumnName,
            string targetHashColumnName,
            string targetIdColumnName,
            ManualResetEvent cancelRequestedEvent);

        void InsertIntoTable<T>(IEnumerable<T> data);
        IEnumerable<T> ReadFromTable<T>();
    }
}