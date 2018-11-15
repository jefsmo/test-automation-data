using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

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
            using (var bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.Default))
            {
                bulkCopy.DestinationTableName = $"[{schema}].[{destinationTable}]";
                bulkCopy.WriteToServer(datatable);

                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"BULK COPY of DATATABLE: '{datatable.TableName}' " +
                        $"to  SQL TABLE: [{schema}].[{destinationTable}] completed.");
                }
            }
        }

        /// <summary>
        /// Compares rows in the expected table to rows in the actual table and returns a DataTable with the differences.
        /// Returns null if no differences found.
        /// </summary>
        /// <param name="expected">A DataTable with the expected data</param>
        /// <param name="actual">A DataTable with the actual data</param>
        /// <param name="primaryKeyColumns">An array of primary key columns</param>
        /// <returns>Returns a DataTable with the differences or null</returns>
        public static DataTable CompareDataTables(DataTable expected, DataTable actual)
        {
            if (Debugger.IsAttached)
            {
                expected.PrintDataTable();
                Console.WriteLine($"{expected.TableName} Primary Keys: {string.Join(", ", expected.PrimaryKey.Select(x => x.ColumnName).ToArray())}");
                actual.PrintDataTable();
                Console.WriteLine($"{actual.TableName} Primary Keys: {string.Join(", ", actual.PrimaryKey.Select(x => x.ColumnName).ToArray())}");
            }

            if (actual.Rows.Count > expected.Rows.Count)
            {
                throw new ArgumentException($"ERROR: Row counts do not match.\r\n" +
                    $"Actual table contains more rows than expected table.\r\n" +
                    $"Expected: [{expected.Rows.Count}] Actual: [{actual.Rows.Count}]");
            }

            var diffs = CreateTableAndSchema("Diffs", expected.PrimaryKey.Select(x => x.Ordinal).ToArray());

            for (var row = 0; row < expected.Rows.Count; row++)
            {
                var primaryKey = new object[expected.PrimaryKey.Length];
                for (var i = 0; i < expected.PrimaryKey.Length; i++)
                {
                    primaryKey[i] = expected.Rows[row].ItemArray[expected.PrimaryKey[i].Ordinal];
                }

                if (actual.Rows.Contains(primaryKey))
                {
                    for (var col = 0; col < expected.Columns.Count; col++)
                    {
                        var actualType = actual.Rows[row].ItemArray[col].GetType();
                        var expectedType = expected.Rows[row].ItemArray[col].GetType();

                        if (actualType.Equals(expectedType))
                        {
                            if (!actual.Rows[row].ItemArray[col].Equals(expected.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, primaryKey);
                            }
                        }
                        else if (expectedType.Equals(typeof(double)) && actualType.Equals(typeof(decimal)))
                        {
                            if (!decimal.ToDouble((decimal)actual.Rows[row].ItemArray[col]).Equals(expected.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, primaryKey);
                            }
                        }
                        else if (actualType.Equals(typeof(double)) && expectedType.Equals(typeof(decimal)))
                        {
                            if (!decimal.ToDouble((decimal)expected.Rows[row].ItemArray[col]).Equals(actual.Rows[row].ItemArray[col]))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, primaryKey);
                            }
                        }
                        else
                        {
                            if (!actual.Rows[row].ItemArray[col].ToString().Equals(expected.Rows[row].ItemArray[col].ToString()))
                            {
                                AddDiffRow(expected, actual, diffs, row, col, primaryKey);
                            }
                        }
                    }
                }
                else
                {
                    foreach (DataColumn col in expected.Columns)
                    {
                        AddMissingRow(expected, diffs, row, col.Ordinal, primaryKey);
                    }
                }

            }
            if (Debugger.IsAttached)
            {
                diffs.PrintDataTable();
            }
            return diffs.Rows.Count > 0
                ? diffs
                : null;

        }

        /// <summary>
        /// Prints the DataTable to the console.
        /// </summary>
        /// <param name="table">The DataTable to print</param>
        public static void PrintDataTable(this DataTable table)
        {
            if (table == null)
            {
                Console.WriteLine("\nDataTable argument is null.");
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

        private static void AddDiffRow(DataTable expected, DataTable actual, DataTable diffs, int row, int col, object[] primaryKey)
        {
            var newRow = diffs.NewRow();

            newRow["Keys"] = string.Join(", ", primaryKey);
            newRow["Row"] = row;
            newRow["Column"] = expected.Columns[col].ColumnName;
            newRow["Expected"] = expected.Rows[row].ItemArray[col].ToString();
            newRow["Actual"] = actual.Rows[row].ItemArray[col].ToString();
            newRow["ExpectedType"] = expected.Rows[row].ItemArray[col].GetType().Name;
            newRow["ActualType"] = actual.Rows[row].ItemArray[col].GetType().Name;

            diffs.Rows.Add(newRow);
        }

        private static void AddMissingRow(DataTable expected, DataTable diffs, int row, int col, object[] primaryKey)
        {
            var newRow = diffs.NewRow();

            newRow["Keys"] = string.Join(", ", primaryKey);
            newRow["Row"] = row;
            newRow["Column"] = expected.Columns[col].ColumnName;
            newRow["Expected"] = expected.Rows[row].ItemArray[col].ToString();
            newRow["Actual"] = "--";
            newRow["ExpectedType"] = expected.Rows[row].ItemArray[col].GetType().Name;
            newRow["ActualType"] = "--";

            diffs.Rows.Add(newRow);
        }

        private static DataTable CreateTableAndSchema(string name, int[] primaryKeyColumns)
        {
            var dt = new DataTable(name);

            DataColumn[] cols =
            {
                new DataColumn
                {
                    ColumnName =  "id",
                    DataType = Type.GetType("System.Int32"),
                    AutoIncrement = true,
                    AutoIncrementSeed = 0,
                    AutoIncrementStep = 1,
                    ReadOnly = true
                },
                new DataColumn("Keys"),
                new DataColumn("Row"),
                new DataColumn("Column"),
                new DataColumn("Expected"),
                new DataColumn("Actual"),
                new DataColumn("ExpectedType"),
                new DataColumn("ActualType")
            };

            dt.Columns.AddRange(cols);

            var primaryKey = new DataColumn[primaryKeyColumns.Length];
            for (var i = 0; i < primaryKeyColumns.Length; i++)
            {
                primaryKey[i] = dt.Columns[primaryKeyColumns[i]];
            }
            dt.PrimaryKey = primaryKey;

            return dt;
        }
    }
}
