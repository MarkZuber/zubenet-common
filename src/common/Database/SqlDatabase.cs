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
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ZubeNet.Common.Database
{
    /// <summary>
    ///     This class interoperates with the database.
    ///     It manages connection lifetime, SqlCommand creation, and conversion of results into objects.
    ///     Everything in the system should use the object model and not talk to SQL directly.
    /// </summary>
    public class SqlDatabase : ISqlDatabase
    {
        /// <summary>
        ///     This is a magic number for the SqlException.Number property when the exception means database timeout.
        /// </summary>
        public const int SqlExceptionNumberTimeout = -2;

        /// <summary>
        ///     This is a magic number for the SqlException.Number property when the exception means transaction was deadlocked on
        ///     resources.
        /// </summary>
        public const int SqlExceptionNumberDeadlock = 1205;

        private const int SqlDefaultTimeout = 120;
        private const int MaxDefaultRetries = 3;

        public static readonly SqlDateTime SqlSmallDateTimeMinValue = new SqlDateTime(1900, 1, 1);
        public static readonly SqlDateTime SqlSmallDateTimeMaxValue = new SqlDateTime(2079, 6, 6);

        private readonly string _connectionString;

        public SqlDatabase(string connectionString, int sqlTimeout = SqlDefaultTimeout)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            _connectionString = connectionString;

            SqlTimeout = sqlTimeout;
            SqlConnection = new SqlConnection(connectionString);
            try
            {
                SqlConnection.Open();
            }
            catch (SqlException e)
            {
                string sqlErrors = string.Join(
                    ",",
                    from SqlError error in e.Errors
                    select error.Number);
                throw new SqlConnectionException(
                    $"SqlException trying to open connection: {SqlConnectionException.SanitizeConnectionString(connectionString)}. Errors: '{sqlErrors}'",
                    connectionString,
                    e);
            }
        }

        public IDbConnection DbConnection => SqlConnection;

        public SqlConnection SqlConnection { get; private set; }
        public TextWriter Log { get; set; }
        public int SqlTimeout { get; set; }

        public long GetTableSizeFromSpaceUsedSproc(string tableName)
        {
            Collection<SqlParameter> parameters = new Collection<SqlParameter>
            {
                new SqlParameter("@objname", tableName)
            };

            var result = ExecuteStoredProcToObject<SpaceUsedResult>("sp_spaceused", parameters);
            return result.Rows;
        }

        public SqlCommand PrepareSqlCommand(string sqlQuery, CommandType commandType, ICollection<SqlParameter> inputParameters)
        {
            return PrepareSqlCommand(sqlQuery, commandType, inputParameters, null, SqlTimeout);
        }

        public SqlCommand PrepareSqlCommand(string sqlQuery, CommandType commandType, ICollection<SqlParameter> inputParameters, int sqlTimeout)
        {
            return PrepareSqlCommand(sqlQuery, commandType, inputParameters, null, sqlTimeout);
        }

        public SqlCommand PrepareSqlCommand(
            string sqlQuery,
            CommandType commandType,
            ICollection<SqlParameter> inputParameters,
            Dictionary<string, SqlParameter> outputParameters,
            int sqlTimeout)
        {
            var sqlCommand = new SqlCommand
            {
                CommandText = sqlQuery,
                CommandType = commandType,
                CommandTimeout = sqlTimeout,
                Connection = SqlConnection
            };

            if (inputParameters != null)
            {
                foreach (SqlParameter parameter in inputParameters)
                {
                    sqlCommand.Parameters.Add(parameter);
                }
            }

            if (outputParameters != null)
            {
                foreach (SqlParameter parameter in outputParameters.Values)
                {
                    parameter.Direction = ParameterDirection.Output;
                    sqlCommand.Parameters.Add(parameter);
                }
            }

            return sqlCommand;
        }

        /// <summary>
        ///     Gets an object of type T from the database by looking at its primary key.
        /// </summary>
        /// <typeparam name="T">Type of object to be returned.</typeparam>
        /// <param name="primaryKeyId">The value of the primary key.</param>
        /// <param name="tableName">The name of the table to be queried.</param>
        /// <param name="primaryKeyFieldName">The name of the field representing the primary key.</param>
        /// <returns>An object of type T that represents the object.  If the object is not found, it returns default(T).</returns>
        public T GetRowByPrimaryKeyId<T>(int primaryKeyId, string tableName, string primaryKeyFieldName)
            where T : class, new()
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter($"@{primaryKeyFieldName}", primaryKeyId)
            };

            string query = $"SELECT * FROM [dbo].[{tableName}] WHERE {primaryKeyFieldName}=@{primaryKeyFieldName}";
            return ExecuteQueryToObject<T>(query, parameters);
        }

        public void ExecuteNonQuery(string query)
        {
            ExecuteNonQuery(query, null, null);
        }

        public void ExecuteNonQuery(string query, ManualResetEvent cancelRequested)
        {
            ExecuteNonQuery(query, null, cancelRequested);
        }

        public void ExecuteNonQuery(string query, Collection<SqlParameter> inputParameters)
        {
            ExecuteNonQuery(query, inputParameters, null);
        }

        public void ExecuteNonQuery(string query, Collection<SqlParameter> inputParameters, ManualResetEvent cancelRequested)
        {
            using (SqlCommand command = PrepareSqlCommand(query, CommandType.Text, inputParameters, null, SqlTimeout))
            {
                ExecuteCancelableNonQueryCommand(command, cancelRequested);
            }
        }

        /// <summary>
        ///     Runs a stored procedure that only takes input parameters.
        /// </summary>
        /// <param name="storedProc">The name of the stored procedure.</param>
        /// <param name="inputParameters">A list of SqlParameter objects representing the input parameters to the stored procedure.</param>
        public void ExecuteStoredProc(string storedProc, Collection<SqlParameter> inputParameters)
        {
            ExecuteStoredProc(storedProc, inputParameters, null, null);
        }

        /// <summary>
        ///     Runs a stored proceedure that takes input parameters and returns output parameters.
        ///     The output parameters are in a Dictionary using a helpful name as the key and the SqlParameter as the value to
        ///     allow
        ///     simple retrieval of the output parameters after calling RunStoredProc().
        /// </summary>
        /// <param name="storedProc">The name of the stored procedure.</param>
        /// <param name="inputParameters">A list of SqlParameter objects representing the input parameters to the stored procedure.</param>
        /// <param name="outputParameters">Dictionary mapping names to output parameters so they can be found easily later.</param>
        public void ExecuteStoredProc(string storedProc, Collection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters)
        {
            ExecuteStoredProc(storedProc, inputParameters, outputParameters, null);
        }

        public void ExecuteStoredProc(
            string storedProc,
            Collection<SqlParameter> inputParameters,
            Dictionary<string, SqlParameter> outputParameters,
            ManualResetEvent cancelRequested)
        {
            using (SqlCommand command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, inputParameters, outputParameters, SqlTimeout))
            {
                ExecuteCancelableNonQueryCommand(command, cancelRequested);
            }
        }

        /// <summary>
        ///     Executes a query that will return a list of 0 or more objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        public IEnumerable<T> ExecuteQueryToObjects<T>(string sqlQuery)
        {
            return ExecuteQueryToObjects<T>(sqlQuery, null);
        }

        public void ExecuteQueryToObjects<T1, T2>(string sqlQuery, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2)
        {
            ExecuteQueryToObjects(sqlQuery, null, out resultSet1, out resultSet2);
        }

        public void ExecuteQueryToObjects<T1, T2, T3>(string sqlQuery, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2, out IEnumerable<T3> resultSet3)
        {
            ExecuteQueryToObjects(sqlQuery, null, out resultSet1, out resultSet2, out resultSet3);
        }

        /// <summary>
        ///     Executes a query that will return a list of 0 or more objects of type T.  This function takes a list of
        ///     SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        public IEnumerable<T> ExecuteQueryToObjects<T>(string sqlQuery, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                return ExecuteSqlCommandToObjects<T>(command);
            }
        }

        public void ExecuteQueryToObjects<T1, T2>(string sqlQuery, ICollection<SqlParameter> parameters, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                ExecuteSqlCommandToObjects(command, out resultSet1, out resultSet2);
            }
        }

        public void ExecuteQueryToObjects<T1, T2, T3>(
            string sqlQuery,
            ICollection<SqlParameter> parameters,
            out IEnumerable<T1> resultSet1,
            out IEnumerable<T2> resultSet2,
            out IEnumerable<T3> resultSet3)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                ExecuteSqlCommandToObjects(command, out resultSet1, out resultSet2, out resultSet3);
            }
        }

        /// <summary>
        ///     Executes a query that will return a single object of type T (or default(T)) if nothing is found.
        ///     This function takes a list of SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="sqlQuery">The query to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An object of type T or default(T) if nothing is found.</returns>
        public T ExecuteQueryToObject<T>(string sqlQuery, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                return ExecuteSqlCommandToObject<T>(command);
            }
        }

        /// <summary>
        ///     Executes a stored proc that will return a list of 0 or more objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="storedProc">The name of the stored proc to run</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        public IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc)
        {
            return ExecuteStoredProcToObjects<T>(storedProc, null);
        }

        /// <summary>
        ///     Executes a stored proc that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to run.</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        public void ExecuteStoredProcToObjects<T1, T2>(string storedProc, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2)
        {
            ExecuteStoredProcToObjects(storedProc, null, out resultSet1, out resultSet2);
        }

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
        public void ExecuteStoredProcToObjects<T1, T2, T3>(string storedProc, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2, out IEnumerable<T3> resultSet3)
        {
            ExecuteStoredProcToObjects(storedProc, null, out resultSet1, out resultSet2, out resultSet3);
        }

        /// <summary>
        ///     Executes a stored proc that will return a list of 0 or more objects of type T.  This function takes a list of
        ///     SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="storedProc">The name of the stored proc to run</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An IEnumerable of the object(s) found in the query.</returns>
        public IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, parameters, SqlTimeout))
            {
                return ExecuteSqlCommandToObjects<T>(command);
            }
        }

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
        public void ExecuteStoredProcToObjects<T1, T2>(string storedProc, ICollection<SqlParameter> parameters, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, parameters, SqlTimeout))
            {
                ExecuteSqlCommandToObjects(command, out resultSet1, out resultSet2);
            }
        }

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
        public void ExecuteStoredProcToObjects<T1, T2, T3>(
            string storedProc,
            ICollection<SqlParameter> parameters,
            out IEnumerable<T1> resultSet1,
            out IEnumerable<T2> resultSet2,
            out IEnumerable<T3> resultSet3)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, parameters, SqlTimeout))
            {
                ExecuteSqlCommandToObjects(command, out resultSet1, out resultSet2, out resultSet3);
            }
        }

        /// <summary>
        ///     Executes a stored proc that will return a single object of type T (or default(T)) if nothing is found.
        ///     This function takes a list of SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="storedProc">The name of the stored procedure to execute.</param>
        /// <param name="parameters">List of SqlParameters that are put into the query.</param>
        /// <returns>An object of type T or default(T) if nothing is found.</returns>
        public T ExecuteStoredProcToObject<T>(string storedProc, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, parameters, SqlTimeout))
            {
                return ExecuteSqlCommandToObject<T>(command);
            }
        }

        public IEnumerable<T> ExecuteStoredProcToObjects<T>(string storedProc, Collection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, inputParameters, outputParameters, SqlTimeout))
            {
                return ExecuteSqlCommandToObjects<T>(command);
            }
        }

        public DataSet ExecuteQueryToDataSet(string sqlQuery, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                return ExecuteSqlCommandToDataSet(command);
            }
        }

        public DataSet ExecuteStoredProcToDataSet(string storedProc, ICollection<SqlParameter> inputParameters, Dictionary<string, SqlParameter> outputParameters)
        {
            using (var command = PrepareSqlCommand(storedProc, CommandType.StoredProcedure, inputParameters, outputParameters, SqlTimeout))
            {
                return ExecuteSqlCommandToDataSet(command);
            }
        }

        public SqlConnectionStats GetConnectionStats()
        {
            var statsQuery = "SELECT cpu_time AS CpuTime, reads, writes, logical_reads AS LogicalReads FROM sys.dm_exec_sessions";

            return ExecuteQueryToObject<SqlConnectionStats>(statsQuery, null);
        }

        /// <summary>
        ///     Executes the query, and returns the first column of the first row in the result set returned by the query.
        ///     Additional columns or rows are ignored.
        /// </summary>
        /// <param name="sqlQuery">The query to execute, such as "SELECT COUNT(*) FROM..."</param>
        /// <param name="parameters">A list of SqlParameters to use with the query.</param>
        /// <returns>The first column of the first row in the result set, or a null reference if the result set is empty.</returns>
        public object ExecuteQueryToScalar(string sqlQuery, ICollection<SqlParameter> parameters)
        {
            using (var command = PrepareSqlCommand(sqlQuery, CommandType.Text, parameters, SqlTimeout))
            {
                int numAttempts = 0;
                while (true)
                {
                    numAttempts++;
                    try
                    {
                        using (var log = new SqlCommandLogger(Log, command))
                        {
                            object scalar = command.ExecuteScalar();
                            return scalar;
                        }
                    }
                    catch (SqlException sqlex)
                    {
                        if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                        {
                            Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public DataTable CreateDataTable(string tableName, IEnumerable<ColumnInfo> columnInfos)
        {
            return CreateDataTableStatic(tableName, columnInfos);
        }

        public string CreateTempTableFromColumnInfos(string tableName, string tableUniquenessKey, IEnumerable<ColumnInfo> columnInfos)
        {
            string tempTableName = $"{tableName}_{tableUniquenessKey}";
            return CreateTableFromColumnInfos(tempTableName, columnInfos);
        }

        public void CreateAndInsertTempTable(string tableName, DataTable dataTable, bool tableLock)
        {
            var columnInfos = new List<ColumnInfo>();
            foreach (DataColumn c in dataTable.Columns)
            {
                columnInfos.Add(new ColumnInfo(c.ColumnName, c.DataType));
            }
            CreateTableFromColumnInfos(tableName, columnInfos);
            BulkInsertDataTable(tableName, dataTable, tableLock);
        }

        public void BulkInsertDataTable(string tableName, DataTable dataTable, bool tableLock, bool identityInsert)
        {
            SqlBulkCopyOptions options = tableLock ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default;
            options |= identityInsert ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(SqlConnection, options, null))
            {
                bulkCopy.ColumnMappings.Clear();
                foreach (DataColumn c in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                }

                bulkCopy.BatchSize = 2000;
                bulkCopy.BulkCopyTimeout = 600; // 10 minutes.  Default of 30 seconds is not sufficient under load.
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.WriteToServer(dataTable);
            }
        }

        public void BulkInsertDataTable(string tableName, DataTable dataTable, bool tableLock)
        {
            BulkInsertDataTable(tableName, dataTable, tableLock, false);
        }

        public void BulkInsertDataTable(string tableName, DataTable dataTable)
        {
            BulkInsertDataTable(tableName, dataTable, false);
        }

        public void BulkInsertDataTable(string tableName, string tableUniquenessKey, DataTable dataTable, IEnumerable<ColumnInfo> columnInfos)
        {
            var infos = columnInfos as IList<ColumnInfo> ?? columnInfos.ToList();
            string tempTableName = CreateTempTableFromColumnInfos(tableName, tableUniquenessKey, infos);
            try
            {
                BulkInsertDataTable(tempTableName, dataTable);
            }
            finally
            {
                string columnNamesTogether = string.Join(", ", infos.Select(columnInfo => columnInfo.ColumnName).ToArray());
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("INSERT INTO [dbo].[{0}] ", tableName);
                sb.AppendFormat("({0}) ", columnNamesTogether);
                sb.AppendFormat("SELECT {0} ", columnNamesTogether);
                sb.AppendFormat("FROM [dbo].[{0}]; ", tempTableName);
                sb.AppendFormat("DROP TABLE [dbo].[{0}]; ", tempTableName);
                ExecuteNonQuery(sb.ToString());
            }
        }

        public void BulkInsertDataTable(string tableName, IDataReader dataReader, bool tableLock)
        {
            BulkInsertDataTable(tableName, dataReader, tableLock, false);
        }

        public void BulkInsertDataTable(string tableName, IDataReader dataReader)
        {
            BulkInsertDataTable(tableName, dataReader, false);
        }

        public void BulkInsertDataTable(string tableName, IDataReader dataReader, bool tableLock, bool identityInsert)
        {
            SqlBulkCopyOptions options = tableLock ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default;
            options |= identityInsert ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(SqlConnection, options, null))
            {
                bulkCopy.BatchSize = 2000;
                bulkCopy.BulkCopyTimeout = 600; // 10 minutes.  Default of 30 seconds is not sufficient under load.
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.WriteToServer(dataReader);
            }
        }

        public void DropTable(string tableName)
        {
            // Drop table is often called in a finally block after an error.  If there was a timeout or other error,
            // the original connection may be closed.  So ensure the connection is opened before dropping the table.
            if (SqlConnection.State != ConnectionState.Open)
            {
                SqlConnection = new SqlConnection(_connectionString);
                SqlConnection.Open();
            }

            StringBuilder query = new StringBuilder();
            query.Append("IF ( ");
            query.AppendFormat("EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{0}') AND type in (N'U')) ", tableName);
            query.AppendFormat("OR OBJECT_ID('tempdb..{0}') IS NOT NULL ", tableName);
            query.AppendFormat(") DROP TABLE {0} ", tableName);

            ExecuteNonQuery(query.ToString());
        }

        public bool CheckParameterExists(string sprocName, string parameterName)
        {
            var query = new StringBuilder();
            query.Append("SELECT COUNT(*) FROM INFORMATION_SCHEMA.PARAMETERS ");
            query.AppendFormat("WHERE SPECIFIC_NAME = N'{0}' AND PARAMETER_NAME = N'@{1}' ", sprocName, parameterName);

            int parameterCount = ExecuteQueryToScalar<int>(query.ToString(), null);
            return parameterCount > 0;
        }

        public void MergeExplicitStringsIntoLookupTable(IEnumerable<string> explicitStrings, string targetTableName, string targetColumnName, ManualResetEvent cancelRequestedEvent)
        {
            var explicitStringsList = explicitStrings as IList<string> ?? explicitStrings.ToList();
            if (!explicitStringsList.Any())
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE #TmpLookup ({0} VARCHAR(MAX)); ", targetColumnName);
            sb.AppendFormat("INSERT INTO #TmpLookup ({0}) ", targetColumnName);
            sb.AppendFormat("VALUES {0}; ", string.Join(",", explicitStringsList.Select(x => $"('{x}')").ToArray()));

            sb.AppendFormat("MERGE INTO [{0}] WITH (TABLOCK) as trg ", targetTableName);
            sb.AppendFormat("  USING (SELECT DISTINCT([{0}]) FROM #TmpLookup) AS src ([{0}]) ", targetColumnName);
            sb.AppendFormat("  ON (trg.[{0}] = src.[{1}]) ", targetColumnName, targetColumnName);
            sb.Append("  WHEN NOT MATCHED THEN ");
            sb.AppendFormat("    INSERT ([{0}]) VALUES ([{1}]); ", targetColumnName, targetColumnName);

            ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);
        }

        public void MergeStringsIntoLookupTableIgnoreNulls(
            string sourceTableName,
            string sourceColumnName,
            string targetTableName,
            string targetColumnName,
            ManualResetEvent cancelRequestedEvent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("MERGE INTO [{0}] WITH (TABLOCK) as trg ", targetTableName);
            sb.AppendFormat("  USING (SELECT DISTINCT([{0}]) FROM [{1}]) AS src ([{0}]) ", sourceColumnName, sourceTableName);
            sb.AppendFormat("  ON (trg.[{0}] = src.[{1}]) ", targetColumnName, sourceColumnName);
            sb.Append("  WHEN NOT MATCHED  AND  ");
            sb.AppendFormat(" src.[{0}] IS NOT NULL", sourceColumnName);
            sb.AppendFormat("  THEN  INSERT ([{0}]) VALUES ([{1}]); ", targetColumnName, sourceColumnName);

            ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);
        }

        public void MergeStringsIntoLookupTable(
            string sourceTableName,
            string sourceColumnName,
            string targetTableName,
            string targetColumnName,
            ManualResetEvent cancelRequestedEvent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("MERGE INTO [{0}] WITH (TABLOCK) as trg ", targetTableName);
            sb.AppendFormat("  USING (SELECT DISTINCT([{0}]) FROM [{1}]) AS src ([{0}]) ", sourceColumnName, sourceTableName);
            sb.AppendFormat("  ON (trg.[{0}] = src.[{1}]) ", targetColumnName, sourceColumnName);
            sb.Append("  WHEN NOT MATCHED THEN ");
            sb.AppendFormat("    INSERT ([{0}]) VALUES ([{1}]); ", targetColumnName, sourceColumnName);

            ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);
        }

        public void MergeStringsIntoLookupTableWithHash(
            string sourceTableName,
            string sourceColumnName,
            string targetTableName,
            string targetColumnName,
            string targetHashColumnName,
            ManualResetEvent cancelRequestedEvent)
        {
            var unmatchedTableName = $"#TmpUnmatchedMergeWithHash_{sourceTableName}";
            DropTable(unmatchedTableName);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT DISTINCT src.[{0}], src.[{0}Hash] INTO [{1}] ", sourceColumnName, unmatchedTableName);
            sb.AppendFormat("FROM [{0}] src ", sourceTableName);
            sb.AppendFormat("LEFT JOIN [{0}] trg ", targetTableName);
            sb.AppendFormat("  ON (trg.[{0}] = src.[{1}Hash] AND trg.[{2}] = src.[{1}]) ", targetHashColumnName, sourceColumnName, targetColumnName);
            sb.AppendFormat("WHERE trg.[{0}] IS NULL ", targetHashColumnName);
            sb.AppendFormat("ORDER BY src.[{0}]", sourceColumnName);
            ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);

            sb.Clear();
            bool tableNotEmpty = ExecuteQueryToScalar<int?>($"SELECT TOP 1 1 FROM {unmatchedTableName}", null).HasValue;
            if (tableNotEmpty)
            {
                sb.AppendFormat("MERGE INTO [{0}] WITH (TABLOCK) as trg ", targetTableName);
                sb.AppendFormat("  USING (SELECT [{0}], [{0}Hash] FROM [{1}]) AS src ([{0}], [{0}Hash]) ", sourceColumnName, unmatchedTableName);
                sb.AppendFormat("  ON (trg.[{0}] = src.[{1}Hash] AND trg.[{2}] = src.[{1}]) ", targetHashColumnName, sourceColumnName, targetColumnName);
                sb.Append("  WHEN NOT MATCHED THEN ");
                sb.AppendFormat("    INSERT ([{0}], [{1}]) VALUES ([{2}], [{2}Hash]); ", targetColumnName, targetHashColumnName, sourceColumnName);
                ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);
            }

            DropTable(unmatchedTableName);
        }

        public void MergeStringsWithMultiColumnsIntoLookupTableWithHash(
            string sourceTableName,
            string sourceStringColumnName,
            IEnumerable<string> sourceOtherColumnNames,
            string targetTableName,
            string targetStringColumnName,
            IEnumerable<string> targetOtherColumnNames,
            string targetHashColumnName,
            ManualResetEvent cancelRequestedEvent)
        {
            if (sourceOtherColumnNames == null)
            {
                throw new ArgumentNullException(nameof(sourceOtherColumnNames));
            }
            if (targetOtherColumnNames == null)
            {
                throw new ArgumentNullException(nameof(targetOtherColumnNames));
            }
            var otherColumnNamesSource = sourceOtherColumnNames as IList<string> ?? sourceOtherColumnNames.ToList();
            var otherColumnNamesTarget = targetOtherColumnNames as IList<string> ?? targetOtherColumnNames.ToList();
            if (otherColumnNamesSource.Count != otherColumnNamesTarget.Count)
            {
                throw new ArgumentException("sourceOtherColumnNames has a different count from targetOtherColumnNames", nameof(targetOtherColumnNames));
            }

            var sourceOtherColumnNamesEnclosed = otherColumnNamesSource.Select(o => o.EncloseBrackets()).ToList();
            var targetOtherColumnNamesEnclosed = otherColumnNamesTarget.Select(o => o.EncloseBrackets()).ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("MERGE INTO [{0}] WITH (TABLOCK) as trg ", targetTableName);
            sb.AppendFormat(
                "  USING (SELECT DISTINCT [{0}], {0}Hash, {2} FROM [dbo].[{1}]) AS src ([{0}], [{0}Hash], {2}) ",
                sourceStringColumnName,
                sourceTableName,
                string.Join(",", sourceOtherColumnNamesEnclosed));
            sb.AppendFormat("  ON (trg.{0} = src.{1}Hash AND trg.{2} = src.{1}) ", targetHashColumnName, sourceStringColumnName, targetStringColumnName);
            sb.Append("  WHEN NOT MATCHED THEN ");
            sb.AppendFormat(
                "    INSERT ({0}, {1}, {2}) VALUES ({3}, {3}Hash, {4}); ",
                targetStringColumnName,
                targetHashColumnName,
                string.Join(",", targetOtherColumnNamesEnclosed),
                sourceStringColumnName,
                string.Join(",", sourceOtherColumnNamesEnclosed));

            ExecuteNonQuery(sb.ToString(), cancelRequestedEvent);
        }

        public void BulkInsertAndMergeStringList(
            string baseTableName,
            string tableNameUniquenessDifferentiator,
            IEnumerable<string> stringsToMerge,
            string sourceColumnType,
            string targetTableName,
            string targetColumnName,
            string targetHashColumnName,
            string targetIdColumnName,
            ManualResetEvent cancelRequestedEvent)
        {
            // Need this to be more unique for the seed than just TmpFunctionNames and TraceRecordId
            // because there could be more than one Qtd task extracting stacks from the same trace at the same time.
            // In fact, it's quite likely to be that way we parallelize qtd execution against an incoming trace.
            // So we're using the base table name for the root parse of the summary table name here as well to distinguish it...
            string baseMergeTableName = "TmpVarCharMerge_" + baseTableName;

            var sourceColumnInfos = new List<ColumnInfo>
            {
                new ColumnInfo("SourceColumn", typeof(string), sourceColumnType, false)
                {
                    ShouldAddHashColumn = true
                }
            };

            var funcdt = CreateDataTable(baseMergeTableName, sourceColumnInfos);

            foreach (string s in stringsToMerge.OrderBy(x => x))
            {
                var dr = funcdt.NewRow();
                dr["SourceColumn"] = s;
                funcdt.Rows.Add(dr);
            }

            int initialSqlTimeout = SqlTimeout;
            SqlTimeout = 1200; // 20 minutes

            string tempMergeTableName = CreateTempTableFromColumnInfos(baseMergeTableName, tableNameUniquenessDifferentiator, sourceColumnInfos);

            try
            {
                ExecuteNonQuery($"CREATE CLUSTERED INDEX [IX_{tempMergeTableName}] on [{tempMergeTableName}] (SourceColumnHash) ON [Primary]");

                BulkInsertDataTable(tempMergeTableName, funcdt, true);

                MergeStringsIntoLookupTableWithHash(tempMergeTableName, "SourceColumn", targetTableName, targetColumnName, targetHashColumnName, cancelRequestedEvent);
            }
            finally
            {
                SqlTimeout = initialSqlTimeout;
                DropTable(tempMergeTableName);
            }
        }

        public void InsertIntoTable<T>(IEnumerable<T> data)
        {
            var sb = new StringBuilder();

            foreach (var datum in data)
            {
                sb.AppendLine(BuildRowInsertString(datum));
            }

            ExecuteNonQuery(sb.ToString());
        }

        public IEnumerable<T> ReadFromTable<T>()
        {
            var columnNames = new List<string>();

            var tableType = typeof(T);

            foreach (var propertyInfo in tableType.GetProperties())
            {
                // Want to add this column to the query list if it has the [Column] attribute
                if (Attribute.IsDefined(propertyInfo, typeof(ColumnAttribute)))
                {
                    var columnAttribute = (ColumnAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(ColumnAttribute));
                    var columnName = string.IsNullOrEmpty(columnAttribute.Name) ? propertyInfo.Name : columnAttribute.Name;
                    columnNames.Add(columnName);
                }
            }

            var tableName = GetTableName(tableType);
            return ExecuteQueryToObjects<T>($"SELECT {string.Join(", ", columnNames.ToArray())} FROM {tableName}");
        }

        public T ExecuteQueryToScalar<T>(string sqlQuery, ICollection<SqlParameter> parameters)
        {
            object scalar = ExecuteQueryToScalar(sqlQuery, parameters);
            if (scalar == null || scalar == DBNull.Value)
            {
                if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    scalar = null;
                }
                else
                {
                    throw new InvalidOperationException("Cannot assign a null value to a non-nullable type");
                }
            }
            return (T)scalar;
        }

        public static DataTable CreateDataTableStatic(string tableName, IEnumerable<ColumnInfo> columnInfos)
        {
            var dt = new DataTable(tableName)
            {
                Locale = CultureInfo.InvariantCulture
            };

            foreach (var columnInfo in columnInfos)
            {
                dt.Columns.Add(columnInfo.ColumnName, columnInfo.ClrDataType);
            }

            return dt;
        }

        /// <summary>
        ///     Merges two DataSets
        /// </summary>
        /// <param name="toDataSet">DataSet to append to</param>
        /// <param name="fromDataSet">DataSet to append from</param>
        public static void AppendDataSet(DataSet toDataSet, DataSet fromDataSet)
        {
            // append all tables from ds2 to ds1
            foreach (DataTable table in fromDataSet.Tables)
            {
                // if destination table name exists, find a new one
                if (toDataSet.Tables.Contains(table.TableName))
                {
                    //
                    // find an unused table name
                    //
                    int tableName = 0;
                    while (toDataSet.Tables.Contains("Table" + tableName.ToString(CultureInfo.InvariantCulture)))
                    {
                        tableName++;
                    }

                    table.TableName = "Table" + tableName.ToString(CultureInfo.InvariantCulture);
                }

                toDataSet.Tables.Add(table.Copy());
            }
        }

        /// <summary>
        ///     Renames the first table in a dataset
        /// </summary>
        /// <param name="results">DataSet to rename the table</param>
        /// <param name="tableName">Name of first table</param>
        public static void RenameDataSetTable(DataSet results, string tableName)
        {
            if (results.Tables.Count > 0)
            {
                results.Tables[0].TableName = tableName;
            }
        }

        /// <summary>
        ///     Renames tables in a dataset from a list of names
        /// </summary>
        /// <param name="results">DataSet to rename the table</param>
        /// <param name="tableNames">List of table names</param>
        public static void RenameDataSetTables(DataSet results, string[] tableNames)
        {
            // throw an exception if count doesn't match up
            if (results.Tables.Count != tableNames.Length)
            {
                throw new InvalidDataException("Table names do not match up to table count");
            }

            for (int i = 0; i < results.Tables.Count; i++)
            {
                // only rename this table if we have enough names
                if (i < tableNames.Length)
                {
                    results.Tables[i].TableName = tableNames[i];
                }
            }
        }

        public static string ReplaceSqlParameters(string query, Collection<SqlParameter> parameters)
        {
            var sb = new StringBuilder(query);

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    string valueString;
                    var parType = parameter.Value.GetType();

                    //
                    // Need to enclose string and DateTime types in single quotes
                    //
                    if (parType == typeof(string) || parType == typeof(DateTime))
                    {
                        valueString = parameter.Value.ToString().EncloseSingleQuotes();
                    }
                    else
                    {
                        valueString = parameter.Value.ToString();
                    }

                    sb.Replace(parameter.ParameterName, valueString);
                }
            }

            return sb.ToString();
        }

        private void ExecuteCancelableNonQueryCommand(SqlCommand command, ManualResetEvent cancelRequested)
        {
            int numAttempts = 0;
            while (true)
            {
                try
                {
                    numAttempts++;
                    using (var logger = new SqlCommandLogger(Log, command))
                    {
                        command.ExecuteNonQuery();
                        return;
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Executes a SqlCommand that will return a list of 0 or more objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of object(s) to return.</typeparam>
        /// <param name="sqlCommand">SqlCommand to run</param>
        /// <returns>An IEnumerable of the object(s) found in the query</returns>
        private IEnumerable<T> ExecuteSqlCommandToObjects<T>(SqlCommand sqlCommand)
        {
            int numAttempts = 0;
            while (true)
            {
                numAttempts++;
                try
                {
                    using (var logger = new SqlCommandLogger(Log, sqlCommand))
                    {
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            return MapRowsToObjects<T>(reader);
                        }
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Executes a SqlCommand that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <param name="sqlCommand">SqlCommand to run</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        private void ExecuteSqlCommandToObjects<T1, T2>(SqlCommand sqlCommand, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2)
        {
            int numAttempts = 0;
            while (true)
            {
                numAttempts++;
                try
                {
                    using (var logger = new SqlCommandLogger(Log, sqlCommand))
                    {
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            resultSet1 = MapRowsToObjects<T1>(reader);

                            if (!reader.NextResult())
                            {
                                throw new InvalidOperationException("Expected second result set.");
                            }

                            resultSet2 = MapRowsToObjects<T2>(reader);
                            return;
                        }
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Executes a SqlCommand that will return a list of zero or more objects of types T1 and T2, respectively, from two
        ///     result sets.
        /// </summary>
        /// <typeparam name="T1">The type of object(s) to return from the first result set.</typeparam>
        /// <typeparam name="T2">The type of object(s) to return from the second result set.</typeparam>
        /// <typeparam name="T3">The type of object(s) to return from the third result set.</typeparam>
        /// <param name="sqlCommand">SqlCommand to run</param>
        /// <param name="resultSet1">Receives an IEnumerable of the object(s) found in the first result set.</param>
        /// <param name="resultSet2">Receives an IEnumerable of the object(s) found in the second result set.</param>
        /// <param name="resultSet3">Receives an IEnumerable of the object(s) found in the third result set.</param>
        private void ExecuteSqlCommandToObjects<T1, T2, T3>(SqlCommand sqlCommand, out IEnumerable<T1> resultSet1, out IEnumerable<T2> resultSet2, out IEnumerable<T3> resultSet3)
        {
            int numAttempts = 0;
            while (true)
            {
                numAttempts++;
                try
                {
                    using (var logger = new SqlCommandLogger(Log, sqlCommand))
                    {
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            resultSet1 = MapRowsToObjects<T1>(reader);

                            if (!reader.NextResult())
                            {
                                throw new InvalidOperationException("Expected second result set.");
                            }

                            resultSet2 = MapRowsToObjects<T2>(reader);

                            if (!reader.NextResult())
                            {
                                throw new InvalidOperationException("Expected third result set.");
                            }

                            resultSet3 = MapRowsToObjects<T3>(reader);
                            return;
                        }
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Executes a SqlCommand that will return a single object of type T (or default(T)) if nothing is found.
        ///     This function takes a list of SqlParameters as input for the query.
        /// </summary>
        /// <typeparam name="T">The type of object to return.</typeparam>
        /// <param name="sqlCommand">The SqlCommand to return the objects, usually a "SELECT * FROM..." query.</param>
        /// <returns>An object of type T or default(T) if nothing is found.</returns>
        private T ExecuteSqlCommandToObject<T>(SqlCommand sqlCommand)
        {
            int numAttempts = 0;
            while (true)
            {
                numAttempts++;
                try
                {
                    using (var log = new SqlCommandLogger(Log, sqlCommand))
                    {
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            return MapRowsToObjects<T>(reader, 1).FirstOrDefault();
                        }
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private DataSet ExecuteSqlCommandToDataSet(SqlCommand sqlCommand)
        {
            int numAttempts = 0;
            while (true)
            {
                numAttempts++;
                try
                {
                    using (var log = new SqlCommandLogger(Log, sqlCommand))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter())
                        {
                            adapter.SelectCommand = sqlCommand;
                            DataSet resultsDataSet = new DataSet
                            {
                                Locale = CultureInfo.InvariantCulture
                            };

                            adapter.Fill(resultsDataSet);
                            return resultsDataSet;
                        }
                    }
                }
                catch (SqlException sqlex)
                {
                    if (sqlex.IsDeadlock() && numAttempts <= MaxDefaultRetries)
                    {
                        Log?.WriteLine("Deadlock occurred on transaction, rerunning transaction");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private string CreateTableFromColumnInfos(string tableName, IEnumerable<ColumnInfo> columnInfos)
        {
            var columnDefinitions = new List<string>();
            foreach (var columnInfo in columnInfos)
            {
                columnDefinitions.AddRange(columnInfo.ToSqlColumnDefinitions());
            }

            DropTable(tableName);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE [{0}] ", tableName);
            sb.Append("( ");
            sb.Append(string.Join(", ", columnDefinitions.ToArray()));
            sb.Append(") ");
            sb.Append("ON [PRIMARY] ");

            ExecuteNonQuery(sb.ToString());
            return tableName;
        }

        // Note that 4 is the internal default initialCapacity for List.
        private IEnumerable<T> MapRowsToObjects<T>(SqlDataReader reader, int initialCapacity = 4)
        {
            List<T> returnObjects = new List<T>(initialCapacity);

            Type type = typeof(T);
            if ((type == typeof(byte)) || (type == typeof(byte[])) || (type == typeof(char)) || (type == typeof(char[])) || (type == typeof(short)) || (type == typeof(int)) ||
                (type == typeof(long)) || (type == typeof(float)) || (type == typeof(double)) || (type == typeof(string)) || (type == typeof(bool)) || (type == typeof(Guid)) ||
                (type == typeof(DateTime)))
            {
                while (reader.Read())
                {
                    returnObjects.Add(MapRowToObjectScalar<T>(reader));
                }
            }
            else
            {
                var properties = FindProperties(typeof(T), reader);

                while (reader.Read())
                {
                    returnObjects.Add(MapRowToObjectOpt<T>(reader, properties));
                }
            }

            return returnObjects;
        }

        private static List<ColumnProperty> FindProperties(Type type, SqlDataReader reader)
        {
            var properties = new List<ColumnProperty>();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                // The ColumnAttribute attribute specifies the mapping.
                if (Attribute.IsDefined(prop, typeof(ColumnAttribute)))
                {
                    int ordinalIndex;
                    // Find out ordinalIndex.
                    {
                        // Get the attribute field name.
                        ColumnAttribute attrib = (ColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));

                        // If a ColumnAttribute field name is not specified, use the property name.
                        string columnName = !string.IsNullOrEmpty(attrib.Name) ? attrib.Name : prop.Name;

                        ordinalIndex = GetOrdinal(reader, columnName);
                    }

                    var cp = new ColumnProperty(prop, ordinalIndex);
                    properties.Add(cp);
                }
            }
            return properties;
        }

        private static int GetOrdinal(SqlDataReader reader, string columnName)
        {
            try
            {
                // If the row contains a column that matches the attribute's field name copy the value to the object.
                return reader.GetOrdinal(columnName);
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
        }

        private static Dictionary<string, int> CreateOrdinalMap(SqlDataReader reader, Type type)
        {
            Dictionary<string, int> ordinalMap = new Dictionary<string, int>();

            // Find all properties of the object type T.
            foreach (PropertyInfo prop in type.GetProperties())
            {
                // The Column attribute specifies the mapping.
                if (Attribute.IsDefined(prop, typeof(ColumnAttribute)))
                {
                    // Get the attribute field name.
                    ColumnAttribute attrib = (ColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));

                    // If a ColumnAttribute field name is not specified, use the property name.
                    string columnName = !string.IsNullOrEmpty(attrib.Name) ? attrib.Name : prop.Name;

                    // If the row contains a column that matches the attribute's field name copy the value to the object.
                    int columnOrdinal = GetOrdinal(reader, columnName);
                    if (columnOrdinal >= 0)
                    {
                        ordinalMap[columnName.ToUpperInvariant()] = columnOrdinal;
                    }
                }
            }
            return ordinalMap;
        }

        private T MapRowToObjectScalar<T>(SqlDataReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            object readerVal = reader[0];
            if (readerVal is DBNull)
            {
                return default(T);
            }

            Type type = typeof(T);
            object result = null;
            if (readerVal is byte[] && type == typeof(long))
            {
                //
                // https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
                // Columns such as rowversion come out as byte[].
                //
                result = MapFieldFromByteArray(reader.GetSqlBinary(0).Value, type);
            }
            else
            {
                // Types here must match the whitelist in MapRowsToObjects<T>().
                if (type == typeof(byte))
                {
                    result = reader.GetByte(0);
                }
                else if (type == typeof(byte[]))
                {
                    result = reader.GetSqlBinary(0).Value;
                }
                else if (type == typeof(char))
                {
                    result = reader.GetChar(0);
                }
                else if (type == typeof(char[]))
                {
                    result = reader.GetString(0).ToCharArray();
                }
                else if (type == typeof(short))
                {
                    result = reader.GetInt16(0);
                }
                else if (type == typeof(int))
                {
                    result = reader.GetInt32(0);
                }
                else if (type == typeof(long))
                {
                    result = reader.GetInt64(0);
                }
                else if (type == typeof(float))
                {
                    result = reader.GetFloat(0);
                }
                else if (type == typeof(double))
                {
                    result = reader.GetDouble(0);
                }
                else if (type == typeof(string))
                {
                    result = reader.GetString(0);
                }
                else if (type == typeof(bool))
                {
                    result = reader.GetBoolean(0);
                }
                else if (type == typeof(Guid))
                {
                    result = reader.GetGuid(0);
                }
                else if (type == typeof(DateTime))
                {
                    result = reader.GetDateTime(0);
                }
                else
                {
                    throw new InvalidCastException($"Unexpected property type {type}.");
                }
            }

            return (T)result;
        }

        private static T MapRowToObjectOpt<T>(SqlDataReader reader, IList<ColumnProperty> properties)
        {
            T item = Activator.CreateInstance<T>();

            // Find all properties of the object type T.
            foreach (ColumnProperty colProp in properties)
            {
                PropertyInfo prop = colProp.PropertyInfo;
                object valAtColumn = null;
                if (colProp.OrdinalIndex >= 0)
                {
                    valAtColumn = reader[colProp.OrdinalIndex];
                }

                if (valAtColumn != null)
                {
                    if (valAtColumn == DBNull.Value)
                    {
                        //
                        // DBNull.Value may be converted trivially. 
                        //
                        prop.SetValue(item, null, null);
                    }
                    else
                    {
                        //
                        // If the type is a Nullable<>, perform the conversion on the underlying type instead.
                        //
                        Type conversionPropertyType;
                        bool nullableValueType;

                        if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            nullableValueType = true;

                            var nullableConverter = new NullableConverter(prop.PropertyType);
                            conversionPropertyType = nullableConverter.UnderlyingType;
                        }
                        else
                        {
                            nullableValueType = false;
                            conversionPropertyType = prop.PropertyType;
                        }

                        //
                        // Perform the conversion.
                        //
                        object convertedValue;

                        if (conversionPropertyType.IsEnum)
                        {
                            if (valAtColumn is string)
                            {
                                convertedValue = Enum.Parse(conversionPropertyType, valAtColumn as string);
                            }
                            else
                            {
                                var enumAsObj = Enum.ToObject(conversionPropertyType, valAtColumn) as Enum;
                                if (enumAsObj.IsValidEnumValue())
                                {
                                    convertedValue = Enum.ToObject(conversionPropertyType, valAtColumn);
                                }
                                else
                                {
                                    throw new FormatException("Undefined value for this enum type");
                                }
                            }
                        }
                        else if (conversionPropertyType == typeof(byte[]))
                        {
                            convertedValue = (byte[])valAtColumn;
                        }
                        else if (conversionPropertyType == typeof(char[]))
                        {
                            convertedValue = ((string)valAtColumn).ToCharArray();
                        }
                        else
                        {
                            //
                            // https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
                            // Columns such as rowversion come out as byte[].
                            //
                            if (valAtColumn is byte[])
                            {
                                convertedValue = MapFieldFromByteArray((byte[])valAtColumn, conversionPropertyType);
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(valAtColumn, conversionPropertyType, CultureInfo.InvariantCulture);

                                if (conversionPropertyType == typeof(DateTime))
                                {
                                    // Since the value is has been converted via Convert.ChangeType, it should be of type DateTime.
                                    // GetCustomAttribute will return null if the attribute does not exist.
                                    if (Attribute.GetCustomAttribute(prop, typeof(DateTimeKindAttribute)) is DateTimeKindAttribute kindAttrib)
                                    {
                                        convertedValue = DateTime.SpecifyKind((DateTime)convertedValue, kindAttrib.DateTimeKind);
                                    }
                                }
                            }
                        }

                        //
                        // Set the property value, taking care to handle Nullable<> values correctly.
                        //
                        if (nullableValueType)
                        {
                            Type nullableSpecialized = typeof(Nullable<>).MakeGenericType(conversionPropertyType);
                            prop.SetValue(item, Activator.CreateInstance(nullableSpecialized, convertedValue), null);
                        }
                        else
                        {
                            prop.SetValue(item, convertedValue, null);
                        }
                    }
                }
            }

            return item;
        }

        private static object MapFieldFromByteArray(byte[] value, Type destinationType)
        {
            if (destinationType == typeof(long))
            {
                return BitConverter.ToInt64(value, 0);
            }
            throw new InvalidCastException($"Unexpected property type {destinationType} to accept a binary value.");
        }

        private static bool IsEnumDefinedEx(Type enumType, int val)
        {
            if (HasFlagsAttribute(enumType))
            {
                bool isDefined = true;
                int curVal = val;
                while (curVal > 0 && isDefined)
                {
                    curVal = Convert.ToInt32(Math.Ceiling(curVal / (double)2));
                    isDefined = Enum.IsDefined(enumType, curVal);
                    if (curVal == 1)
                    {
                        curVal = 0;
                    }
                }

                return isDefined;
            }
            return Enum.IsDefined(enumType, val);
        }

        private static bool HasFlagsAttribute(Type enumType)
        {
            var attributes = TypeDescriptor.GetAttributes(enumType);
            return attributes.OfType<FlagsAttribute>().Any();
        }

        private string GetTableName(Type tableType)
        {
            var tableAttribute = (TableAttribute)Attribute.GetCustomAttribute(tableType, typeof(TableAttribute));

            // Use the Table attribute if it exists, otherwise use the class name
            var tableName = !string.IsNullOrEmpty(tableAttribute?.Name) ? tableAttribute.Name : tableType.Name;

            return tableName;
        }

        private string BuildRowInsertString<T>(T row)
        {
            var columnNames = new List<string>();
            var columnValues = new List<string>();
            var tableType = typeof(T);

            foreach (var propertyInfo in tableType.GetProperties())
            {
                // Want to add this column to the data insert list if it has the [Column] attribute
                if (Attribute.IsDefined(propertyInfo, typeof(ColumnAttribute)))
                {
                    var columnAttribute = (ColumnAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(ColumnAttribute));

                    // Don't want to insert values for DB generated columns!
                    if (!columnAttribute.IsDbGenerated)
                    {
                        var columnName = string.IsNullOrEmpty(columnAttribute.Name) ? propertyInfo.Name : columnAttribute.Name;
                        columnNames.Add(columnName);

                        var propertyValue = propertyInfo.GetValue(row, null);
                        string columnValue;

                        // Determine SQL string to represent this property value.
                        if (propertyValue == null)
                        {
                            columnValue = "NULL";
                        }
                        else
                        {
                            if (propertyInfo.PropertyType.IsEnum)
                            {
                                object e;
                                if (propertyValue is string)
                                {
                                    e = Enum.Parse(propertyInfo.PropertyType, propertyValue as string);
                                }
                                else
                                {
                                    if (!Enum.IsDefined(propertyInfo.PropertyType, propertyValue))
                                    {
                                        throw new FormatException("Undefined value for this enum type");
                                    }
                                    e = Enum.ToObject(propertyInfo.PropertyType, propertyValue);
                                }
                                columnValue = $"{e:D}";
                            }
                            else if (propertyInfo.PropertyType == typeof(string))
                            {
                                // Need to surround a string with single quotes
                                columnValue = ((string)propertyValue).EncloseSingleQuotes();
                            }
                            else if (propertyInfo.PropertyType == typeof(bool))
                            {
                                columnValue = (bool)propertyValue ? "1" : "0";
                            }
                            else if (propertyInfo.PropertyType == typeof(bool?))
                            {
                                columnValue = ((bool?)propertyValue).Value ? "1" : "0";
                            }
                            else if (propertyInfo.PropertyType == typeof(byte[]))
                            {
                                columnValue = "0x" + string.Concat(Array.ConvertAll((byte[])propertyValue, x => x.ToString("X2")));
                            }
                            else if (propertyInfo.PropertyType == typeof(Guid))
                            {
                                columnValue = ((Guid)propertyValue).ToString("B").EncloseSingleQuotes();
                            }
                            else if (propertyInfo.PropertyType == typeof(DateTime))
                            {
                                columnValue = ((DateTime)propertyValue).ToString(CultureInfo.InvariantCulture).EncloseSingleQuotes();
                            }
                            else
                            {
                                columnValue = propertyValue.ToString();
                            }
                        }

                        columnValues.Add(columnValue);
                    }
                }
            }

            var tableName = GetTableName(tableType);
            return $"INSERT INTO {tableName} ({string.Join(", ", columnNames.ToArray())}) VALUES ({string.Join(", ", columnValues.ToArray())})";
        }

        private struct ColumnProperty
        {
            public ColumnProperty(PropertyInfo propertyInfo, int ordinalIndex)
            {
                PropertyInfo = propertyInfo;
                OrdinalIndex = ordinalIndex;
            }

            public PropertyInfo PropertyInfo { get; }

            // If value < 0, implies cannot find the map of name to index.
            public int OrdinalIndex { get; }
        }

        private class SpaceUsedResult
        {
            [Column]
            public string Name { get; set; }

            [Column]
            public long Rows { get; set; }
        }

        private class SqlCommandLogger : IDisposable
        {
            private static readonly string QuerySeparator = Environment.NewLine + Environment.NewLine;
            private readonly Stopwatch _stopwatch;

            public SqlCommandLogger(TextWriter log, SqlCommand command)
            {
                Log = log;
                if (Log != null)
                {
                    _stopwatch = Stopwatch.StartNew();
                    Log.Write(CommandToLogString(command));
                }
            }

            public TextWriter Log { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private string CommandToLogString(SqlCommand command)
            {
                var sb = new StringBuilder();
                sb.AppendLine(command.CommandText);
                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        sb.AppendFormat("-- {0}: {1} [{2}]", param.ParameterName, param.SqlDbType, param.SqlValue);
                    }
                }
                return sb.ToString().Replace(QuerySeparator, Environment.NewLine);
            }

            ~SqlCommandLogger()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Log != null && _stopwatch != null)
                    {
                        _stopwatch.Stop();
                        Log.Write("-- {0}ms{1}", _stopwatch.ElapsedMilliseconds, QuerySeparator);
                    }
                }
            }
        }

        #region IDisposable Members

        private bool _disposed;

        ~SqlDatabase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free managed resources.
                    if (SqlConnection != null)
                    {
                        SqlConnection.Close();
                        SqlConnection = null;
                    }
                }

                // Free unmanaged resources.

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}