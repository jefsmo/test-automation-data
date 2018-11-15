using System;
using System.Data;
using System.Linq;

namespace Test.Automation.Data.Tests
{
    /// <summary>
    /// Represents methods shared by all the test classes.
    /// </summary>
    public static class Common
    {
        public static DataTable CreateDataTable(string name, DataColumn[] primaryKeyColumns)
        {
            var keys = primaryKeyColumns.Select(x => x.Ordinal).ToArray();
            return CreateDataTable(name, keys);
        }

        public static DataTable CreateDataTable(string name, int[] primaryKeyColumns)
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
                new DataColumn("mytext", typeof(string)),
                new DataColumn("myinteger", typeof(int)),
                new DataColumn("mydouble", typeof(double)),
                new DataColumn("mydecimal", typeof(decimal)),
                new DataColumn("computed", typeof(decimal), "mydouble * mydecimal")
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

        public static void AddDataRow(DataTable table, string text, int integerVal, double doubleVal, decimal decimalVal)
        {
            var row = table.NewRow();

            row["mytext"] = text;
            row["myinteger"] = integerVal;
            row["mydouble"] = doubleVal;
            row["mydecimal"] = decimalVal;

            table.Rows.Add(row);
        }
    }
}
