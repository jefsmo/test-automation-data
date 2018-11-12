using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Automation.Data.Tests
{
    /// <summary>
    /// Represents methods shared by all the test classes.
    /// </summary>
    public static class Common
    {
        public static DataTable CreateDataTable(string name)
        {
            var dt = new DataTable(name);

            DataColumn col;

            DataColumn[] cols =
            {
                col = new DataColumn
                {
                    ColumnName =  "id",
                    DataType = Type.GetType("System.Int32"),
                    AutoIncrement = true,
                    AutoIncrementSeed = 0,
                    AutoIncrementStep = 1,
                    ReadOnly = true
                },
                col = new DataColumn("mytext", typeof(string)),
                col = new DataColumn("myinteger", typeof(int)),
                col = new DataColumn("mydouble", typeof(double)),
                col = new DataColumn("mydecimal", typeof(decimal)),
                col = new DataColumn("computed", typeof(decimal), "mydouble * mydecimal")
            };

            dt.Columns.AddRange(cols);
            dt.PrimaryKey = new [] { dt.Columns["id"] };
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
