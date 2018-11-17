using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace Test.Automation.Data
{
    /// <summary>
    /// Represents methods for importing data from files (Excel, CSV, TXT).
    /// REQUIRED: Install Microsoft Access Database Engine 2016 Redistributable.
    /// This is the source of the Microsoft.ACE.OLEDB.16.0 provider.
    /// </summary>
    public static class ImportFileHelper
    {
        public static DataTable ExecuteDataTableFromExcel(string selectCommandText, FileInfo excelFile, DataColumn[] primaryKeyColumns)
        {
            var keyColumns = primaryKeyColumns.Select(x => x.Ordinal).ToArray();
            return ExecuteDataTableFromExcel(selectCommandText, excelFile, keyColumns);
        }

        /// <summary>
        /// Fills a DataTable with data imported from an Excel file.
        /// Example query: SELECT * FROM [Sheet2$]
        /// </summary>
        /// <param name="selectCommandText">The Excel query used to return data</param>
        /// <param name="fileInfo">A FileInfo object for the Excel file</param>
        /// <param name="primaryKeyColumns">An array of the column numbers that make up the primary key</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTableFromExcel(string selectCommandText, FileInfo fileInfo, int[] primaryKeyColumns)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                // For Excel files: the full path of the file.
                DataSource = fileInfo.FullName,
                Provider = "Microsoft.ACE.OLEDB.16.0"
            };
            builder.Add("Extended Properties", $"Excel 12.0 Xml;HDR=YES;IMEX={(int)IMEX.Text};");

            return GetDataUsingOleDb(selectCommandText, fileInfo, primaryKeyColumns, builder);
        }

        public static DataTable ExecuteDataTableFromTextFile(string selectCommandText, FileInfo textFile, DataColumn[] primaryKeyColumns)
        {
            var keyColumns = primaryKeyColumns.Select(x => x.Ordinal).ToArray();
            return ExecuteDataTableFromTextFile(selectCommandText, textFile, keyColumns);
        }
        
        /// <summary>
        /// Fills a DataTable with data imported from a text file.
        /// Example query: SELECT * FROM [textfile#csv]
        /// </summary>
        /// <param name="selectCommandText">The text file query used to return data</param>
        /// <param name="textFile">A FileInfo object for the text file</param>
        /// <param name="primaryKeyColumns">An array of the column numbers that make up the primary key</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTableFromTextFile(string selectCommandText, FileInfo fileInfo, int[] primaryKeyColumns)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                // For text files: must be the path without the file name.
                DataSource = fileInfo.DirectoryName,
                Provider = "Microsoft.ACE.OLEDB.16.0"
            };
            builder.Add("Extended Properties", $"Text;");

            return GetDataUsingOleDb(selectCommandText, fileInfo, primaryKeyColumns, builder);
        }

        /// <summary>
        /// Extracts the table name from a select query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>String</returns>
        public static string GetTableNameFromSelectStatement(string query)
        {
            var name = query
                .ToUpperInvariant()
                .Split(new[] { " ", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                .SkipWhile(x => x != "FROM")
                .Skip(1)
                .FirstOrDefault();

            if (name == null)
            {
                return "SQL RESULT";
            }
            return name;
        }

        #region EXTENDED PROPERTIES
        /// <summary>
        /// Excel Extended Property. IMEX (IMport EXport) mode to use when importing from Excel.
        /// </summary>
        private enum IMEX
        {
            /// <summary>
            /// Columns of mixed data will be cast to the predominant data type on import.
            /// </summary>
            MajorityTypes = 0,

            /// <summary>
            /// (Default) Columns of mixed data will be cast to Text on import.
            /// </summary>
            Text = 1
        }
        #endregion

        #region PRIVATE METHODS
        private static DataTable GetDataUsingOleDb(
            string selectCommandText,
            FileInfo excelFile,
            int[] primaryKeyColumns,
            OleDbConnectionStringBuilder builder)
        {
            var dt = new DataTable(GetTableNameFromSelectStatement(selectCommandText));

            using (var da = new OleDbDataAdapter(selectCommandText, builder.ConnectionString))
            {
                var rows = da.Fill(dt);

                var primaryKey = new DataColumn[primaryKeyColumns.Length];
                for (var i = 0; i < primaryKeyColumns.Length; i++)
                {
                    primaryKey[i] = dt.Columns[primaryKeyColumns[i]];
                }
                dt.PrimaryKey = primaryKey;

                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"\nOLE DB Connection String: {builder.ConnectionString}");
                    Console.WriteLine($"IMPORT from FILE {excelFile.Name} to DATATABLE {dt.TableName}: {rows} rows.");
                }
            }
            return dt;
        }
        #endregion
    }
}
