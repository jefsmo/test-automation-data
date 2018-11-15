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
        /// <param name="excelFile">A FileInfo object for the Excel file</param>
        /// <param name="primaryKeyColumns">An array of the column numbers that make up the primary key</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTableFromExcel(string selectCommandText, FileInfo excelFile, int[] primaryKeyColumns)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                // For Excel files: the full path of the file.
                DataSource = excelFile.FullName,
                Provider = "Microsoft.ACE.OLEDB.16.0"
            };
            builder.Add("Extended Properties", $"Excel 12.0 Xml;HDR=YES;IMEX={(int)IMEX.Text};");

            var dt = new DataTable(excelFile.Name.Replace(excelFile.Extension, ""));

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
                    Console.WriteLine($"Connection String: {builder.ConnectionString}");
                    Console.WriteLine($"EXCEL IMPORT '{selectCommandText.Substring(selectCommandText.IndexOf("FROM")).Trim()}' " +
                        $"to DATATABLE '{excelFile.Name.Replace(excelFile.Extension, "")}': {rows} rows.");
                }
            }
            return dt;
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
        public static DataTable ExecuteDataTableFromTextFile(string selectCommandText, FileInfo textFile, int[] primaryKeyColumns)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                // For text files: must be the path without the file name.
                DataSource = textFile.DirectoryName,
                Provider = "Microsoft.ACE.OLEDB.16.0"
            };
            builder.Add("Extended Properties", $"Text;");

            var dt = new DataTable(textFile.Name.Replace(textFile.Extension, ""));

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
                    Console.WriteLine($"Connection String: {builder.ConnectionString}");
                    Console.WriteLine($"TEXT FILE IMPORT '{selectCommandText.Substring(selectCommandText.IndexOf("FROM")).Trim()}' " +
                        $"to DATATABLE '{textFile.Name.Replace(textFile.Extension, "")}': {rows} rows.");
                }
            }
            return dt;
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
    }
}
