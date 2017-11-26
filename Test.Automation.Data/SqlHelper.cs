using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Test.Automation.Data
{
    /// <summary>
    /// Represents methods for retrieving data from an SQL Server database.
    /// </summary>
    /// <remarks>
    /// Reference: https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand(v=vs.110).aspx
    /// </remarks>
    public static class SqlHelper
    {
        /// <summary>
        /// Executes a Transact-SQL statement against the connection and returns the number of rows affected.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQLServer/database.</param>
        /// <param name="commandText">The SQL command to be executed.</param>
        /// <param name="commandType">Either stored procedure or text.</param>
        /// <param name="parameters">A collection of SQLParameters to be passed to the SQL command.</param>
        /// <returns>The number of rows affected.</returns>
        public static int ExecuteNonQuery(string connectionString, string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(commandText, conn))
            {
                // There're three command types: StoredProcedure, Text, TableDirect. 
                // The TableDirect type is only for OLE DB.  
                cmd.CommandType = commandType;
                if (parameters[0] != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();
                var result = cmd.ExecuteNonQuery();

                if (Debugger.IsAttached)
                {
                    LogNonQueryResult(conn, cmd, result);
                }

                cmd.Parameters.Clear();
                return result;
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query.
        /// Additional rows or columns are ignored.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQLServer/database.</param>
        /// <param name="commandText">The SQL command to be executed.</param>
        /// <param name="commandType">Either stored procedure or text.</param>
        /// <param name="parameters">A collection of SQLParameters to be passed to the SQL command.</param>
        /// <returns>The first column of the first row as an object.</returns>
        public static object ExecuteScalar(string connectionString, string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                if (parameters[0] != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (Debugger.IsAttached)
                {
                    LogScalarResult(conn, cmd, result ?? "- The result set is empty. -");
                }

                cmd.Parameters.Clear();
                return result;
            }
        }

        /// <summary>
        /// Sends the CommandText to the Connection, and builds a SqlDataReader using one of the CommandBehavior values.
        /// When the command is executed, the associated Connection object is closed when the associated DataReader object is closed.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQLServer/database.</param>
        /// <param name="commandText">The SQL command to be executed.</param>
        /// <param name="commandType">Either stored procedure or text.</param>
        /// <param name="parameters">A collection of SQLParameters to be passed to the SQL command.</param>
        /// <returns>A SqlDataReader containing the query results.</returns>
        public static SqlDataReader ExecuteReader(string connectionString, string commandText, CommandType commandType, params SqlParameter[] parameters)
        {
            var conn = new SqlConnection(connectionString);

            using (var cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                cmd.Parameters.AddRange(parameters);

                conn.Open();
                // When using CommandBehavior.CloseConnection, the connection will be closed when the IDataReader is closed.
                var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                cmd.Parameters.Clear();
                return reader;
            }
        }

        /// <summary>
        /// Fills a DataTable with values from the internal ExecuteReader command using the supplied IDataReader.
        /// The ExecuteReader command sends the CommandText to the Connection, and builds a SqlDataReader using CommandBehavior.CloseConnection.
        /// When the command is executed, the associated Connection object is closed when the associated DataReader object is closed.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQLServer/database.</param>
        /// <param name="commandText">The SQL command to be executed.</param>
        /// <param name="commandType">Either stored procedure or text.</param>
        /// <param name="parameters">A collection of SQLParameters to be passed to the SQL command.</param>
        /// <returns>A DataTable containg the results.</returns>
        public static DataTable ExecuteDataTable(string connectionString, string commandText, CommandType commandType,  params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                if (parameters[0] != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                conn.Open();

                var dataTable = new DataTable("SQL RESULT TABLE");
                // When using CommandBehavior.CloseConnection, the connection will be closed when the IDataReader is closed.
                dataTable.Load(cmd.ExecuteReader(CommandBehavior.CloseConnection), LoadOption.OverwriteChanges, FillErrorHandler);
                if (Debugger.IsAttached)
                {
                    LogDataTableResult(conn, cmd, dataTable);
                }

                cmd.Parameters.Clear();
                return dataTable;
            }
        }

        private static void LogDataTableResult(SqlConnection conn, SqlCommand cmd, DataTable table)
        {
            var data = "- No result set returned. -";
            var rowCount = 0;

            if (table != null)
            {
                if (table.Rows.Count > 0)
                {
                    rowCount = table.Rows.Count;
                    data = table.Rows[0].ItemArray.Aggregate((x, next) => x + ", " + next).ToString();
                }
            }
            LogResultInfo(conn, cmd, data, rowCount);
        }

        private static void LogNonQueryResult(SqlConnection conn, SqlCommand cmd, int rowsAffected)
        {
            LogResultInfo(conn, cmd, rowsAffected.ToString(), 0);
        }

        private static void LogScalarResult(SqlConnection conn, SqlCommand cmd, object scalarResult)
        {
            LogResultInfo(conn, cmd, scalarResult.ToString(), 0);
        }

        private static void LogResultInfo(SqlConnection conn, SqlCommand cmd, string result, int rowCount)
        {
            var logInfo = new Dictionary<string, string>
            {
                {"ConnectionString", conn.ConnectionString },
                {"SQL Query", cmd.CommandText},
                {"Result", result}
            };

            if (rowCount > 0)
            {
                logInfo.Add("Row Count", rowCount.ToString());
            }

            var sqlParams = GetParameterData(cmd);

            if (sqlParams.Length > 0)
            {
                logInfo.Add("SQL Parameters", sqlParams.ToString());
            }

            foreach (var kvp in logInfo)
            {
                Console.WriteLine("{0,-25}\t{1,-25}", kvp.Key, kvp.Value);
            }
        }

        private static StringBuilder GetParameterData(SqlCommand cmd)
        {
            var sqlParams = new StringBuilder();
            if (cmd.Parameters.Count > 0)
            {
                foreach (SqlParameter commandParameter in cmd.Parameters)
                {
                    sqlParams.Append($"| {commandParameter.Direction} - {commandParameter.ParameterName} : {commandParameter.Value} ");
                }

                sqlParams.Append("|");
            }
            return sqlParams;
        }

        private static void FillErrorHandler(object sender, FillErrorEventArgs e)
        {
            // You can use the e.Errors value to determine exactly what went wrong.
            if (e.Errors.GetType() == typeof(FormatException))
            {
                Console.WriteLine("Error when attempting to update the value: {0}", e.Values[0]);
            }

            // Setting e.Continue to True tells the Load method to continue trying. 
            // Setting it to False indicates that an error has occurred, and the Load method raises the exception that got you here.
            e.Continue = true;
        }
    }
}