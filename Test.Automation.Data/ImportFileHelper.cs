using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;

namespace Test.Automation.Data
{
    /// <summary>
    /// Represents methods for importing data from files (Excel, CSV, TXT).
    /// REQUIRED: Install Microsoft Access Database Engine 2016 Redistributable.
    /// </summary>
    public static class ImportFileHelper
    {
        /// <summary>
        /// Fills a DataTable with data imported from an Excel file.
        /// Example query: SELECT * FROM [Sheet2$]
        /// </summary>
        /// <param name="query">The Excel query used to return data</param>
        /// <param name="excelFile">A FileInfo object for the Excel file</param>
        /// <param name="timeout">The CommandTimeout value</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTableFromExcel(string query, FileInfo excelFile, int timeout = 30)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                DataSource = excelFile.FullName,
                Provider = "Microsoft.ACE.OLEDB.12.0"
            };
            builder.Add("Extended Properties", $"Excel 12.0 Xml;HDR=YES;IMEX={IMEX.MajorityTypes};");

            var datatable = new DataTable(excelFile.Name.Replace(excelFile.Extension, ""));

            using (var cnn = new OleDbConnection
            {
                ConnectionString = builder.ConnectionString
            })
            using (var cmd = new OleDbCommand()
            {
                Connection = cnn,
                CommandText = query,
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            })
            using (var da = new OleDbDataAdapter(cmd))
            {
                cnn.Open();
                var rows = da.Fill(datatable);

                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"Connection String: {builder.ConnectionString}");
                    Console.WriteLine($"EXCEL IMPORT '{query.Substring(query.IndexOf("FROM")).Trim()}' to DATATABLE '{excelFile.Name.Replace(excelFile.Extension, "")}': {rows} rows.");
                }
            }
            return datatable;
        }

        /// <summary>
        /// Fills a DataTable with data imported from a text file.
        /// Example query: SELECT * FROM [textfile#csv]
        /// </summary>
        /// <param name="query">The text file query used to return data</param>
        /// <param name="textFile">A FileInfo object for the text file</param>
        /// <param name="timeout">The CommandTimeout value</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTableFromTextFile(string query, FileInfo textFile, int timeout = 30)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                // Must be the path without the file name.
                DataSource = textFile.DirectoryName,
                Provider = "Microsoft.ACE.OLEDB.12.0"
            };
            builder.Add("Extended Properties", $"text;");

            var dt = new DataTable(textFile.Name.Replace(textFile.Extension, ""));

            using (var cnn = new OleDbConnection
            {
                ConnectionString = builder.ConnectionString
            })
            using (var cmd = new OleDbCommand()
            {
                Connection = cnn,
                CommandText = query,
                CommandTimeout = timeout,
                CommandType = CommandType.Text
            })
            using (var da = new OleDbDataAdapter(cmd))
            {
                cnn.Open();
                var rows = da.Fill(dt);
                dt.PrimaryKey = new[] { dt.Columns[0] };

                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"Connection String: {builder.ConnectionString}");
                    Console.WriteLine($"TEXT FILE IMPORT '{query.Substring(query.IndexOf("FROM")).Trim()}' to DATATABLE '{textFile.Name.Replace(textFile.Extension, "")}': {rows} rows.");
                }
            }
            return dt;
        }

        #region EXTENDED PROPERTIES
        /// <summary>
        /// Excel only; IMport EXport (IMEX) mode.
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

        /// <summary>
        /// Text file FMT=[value];
        /// For Delimited(*): You can substitute any character for the * except for the double quotation mark (").
        /// </summary>
        private class FMT
        {
            public static string TabDelimited => "TabDelimited";
            public static string CommaDelimited => "CSVDelimited";
            public static string Delimited_Pipe => "Delimited(|)";
            public static string Delimited_SemiColon => "Delimited(;)";
            public static string FixedLength => "FixedLength";
        }
        #endregion
    }
}
