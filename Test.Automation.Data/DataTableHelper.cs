using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Test.Automation.Data
{
    public static class DataTableHelper
    {
        /// <summary>
        /// Copies all the rows in the DataTable argument to a destination table in a SQL database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="datatable"></param>
        /// <param name="destinationTable"></param>
        /// <param name="schema"></param>
        public static void BulkCopyFromDataTable(string connectionString, DataTable datatable, string destinationTable, string schema = "dbo")
        {
            using (var cnn = new SqlConnection(connectionString))
            using (var bulkCopy = new SqlBulkCopy(cnn))
            {
                cnn.Open();
                bulkCopy.DestinationTableName = $"[{schema}].[{destinationTable}]";
                bulkCopy.WriteToServer(datatable);

                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"BULK COPY of DATATABLE: '{datatable.TableName}' to  SQL TABLE: [{schema}].[{destinationTable}] Completed.");
                }
            }
        }

        /// <summary>
        /// Compares rows in the expected table to rows in the actual table and returns a DataTable with the differences.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        public static DataTable CompareDataTables(DataTable expected, DataTable actual)
        {
            if (expected.Rows.Count != actual.Rows.Count)
            {
                Console.WriteLine($"\nWARNING: Row counts do not match.\r\n" +
                    $"Expected: [{expected.Rows.Count}] Actual: [{actual.Rows.Count}]\r\n" +
                    $"Compare only matches rows that exist in both tables.");
            }
            var diffs = CreateDiffTableAndSchema("Diffs");

            for (var row = 0; row < expected.Rows.Count; row++)
            {
                if (actual.Rows.Contains(row))
                {
                    for (var col = 0; col < expected.Columns.Count; col++)
                    {
                        var actualType = actual.Rows[row].ItemArray[col].GetType();
                        var expectedType = expected.Rows[row].ItemArray[col].GetType();

                        if (actualType.Equals(expectedType))
                        {
                            if (!actual.Rows[row].ItemArray[col].Equals(expected.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, expectedType, actualType);
                            }
                        }
                        else if (expectedType.Equals(typeof(double)) && actualType.Equals(typeof(decimal)))
                        {
                            if (!decimal.ToDouble((decimal)actual.Rows[row].ItemArray[col]).Equals(expected.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, expectedType, actualType);
                            }
                        }
                        else if (actualType.Equals(typeof(double)) && expectedType.Equals(typeof(decimal)))
                        {
                            if (!decimal.ToDouble((decimal)expected.Rows[row].ItemArray[col]).Equals(actual.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, expectedType, actualType);
                            }
                        }
                        else
                        {
                            if (!actual.Rows[row].ItemArray[col].ToString().Equals(expected.Rows[row].ItemArray[col].ToString()))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, expectedType, actualType);
                            }
                        }
                    }
                }
            }
            return diffs;
        }

        /// <summary>
        /// Prints the DataTable to the console.
        /// </summary>
        /// <param name="table"></param>
        public static void PrintDataTable(this DataTable table)
        {
            if (table == null)
            {
                Console.WriteLine("\nDataTable is null.");
                return;
            }

            if (table.Rows.Count == 0)
            {
                Console.WriteLine($"\nDataTable '{table.TableName}' is empty.");
                return;
            }

            Console.WriteLine($"\nDataTable Name: {table.TableName}");

            foreach (DataColumn col in table.Columns)
            {
                Console.Write($"{col.ColumnName, -14}\t");
            }
            Console.WriteLine();

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    Console.Write($"{row[col], -14}\t");
                }
                Console.WriteLine();
            }
        }

        private static void AddDiffRow(DataTable expected, DataTable actual, DataTable diffs, int row, int col, Type expectedType, Type actualType)
        {
            var newRow = diffs.NewRow();

            newRow["Row"] = row;
            newRow["Column"] = expected.Columns[col].ColumnName;
            newRow["Expected"] = expected.Rows[row].ItemArray[col].ToString();
            newRow["Actual"] = actual.Rows[row].ItemArray[col].ToString();
            newRow["ExpectedType"] = expectedType;
            newRow["ActualType"] = actualType;

            diffs.Rows.Add(newRow);
        }

        private static DataTable CreateDiffTableAndSchema(string name)
        {
            var dt = new DataTable(name);

            DataColumn col;

            DataColumn[] cols =
            {
                col = new DataColumn("Row"),
                col = new DataColumn("Column"),
                col = new DataColumn("Expected"),
                col = new DataColumn("Actual"),
                col = new DataColumn("ExpectedType"),
                col = new DataColumn("ActualType")
            };

            dt.Columns.AddRange(cols);
            dt.PrimaryKey = new[] { dt.Columns["Row"] };
            return dt;
        }
    }
}
