using System;
using System.Data;
using System.Linq;
using NUnit.Framework;

namespace Test.Automation.Data.Tests
{
    [TestFixture]
    public class DataTableHelperTests
    {
        [Test]
        public void CompareDataTables_ReturnsDiffs_WhenTablesAreDifferent()
        {
            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00, 4.00m);
            Common.AddDataRow(expected, "Text four.", 43, 5.00, 5.00m);

            var actual = Common.CreateDataTable("Actual", expected.PrimaryKey);
            Common.AddDataRow(actual, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(actual, "Text four.", 42, 5.00, 5.00m);
            Common.AddDataRow(actual, "Text three.", 43, 4.00, 4.00m);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            Assert.That(diffs.Rows.Count, Is.EqualTo(10));
        }

        [Test]
        public void CompareDataTables_ReturnsNull_WhenTablesIdentical()
        {
            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00, 4.00m);

            var actual = Common.CreateDataTable("Actual", expected.PrimaryKey);
            Common.AddDataRow(actual, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(actual, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(actual, "Text three.", 43, 4.00, 4.00m);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            Assert.That(diffs, Is.Null);
        }

        [Test]
        public void CompareDataTables_ThrowsExcepton_WhenActualHasMoreRows()
        {
            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00, 4.00m);

            var actual = Common.CreateDataTable("Actual", expected.PrimaryKey);
            Common.AddDataRow(actual, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(actual, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(actual, "Text three.", 43, 4.00, 4.00m);
            Common.AddDataRow(actual, "Text four.", 43, 5.00, 5.00m);

            var ex = Assert.Throws<ArgumentException>(() => DataTableHelper.CompareDataTables(expected, actual));
        }

        [Test]
        public void PrintDataTable_WhenNull()
        {
            var dt = default(DataTable);
            dt.PrintDataTable();
        }

        [Test]
        public void PrintDataTable_WhenEmpty()
        {
            var dt = new DataTable("Foo");
            dt.PrintDataTable();
        }

        [Test]
        public void GetTableFromQuery_ShouldReturnTableName_WhenSelectStatement()
            {
            const string sql = @"
SELECT
    id
    ,mytext
    ,foo
    ,bar
from [Database].[schema].[table] t
WHERE
    id = 1;
";
            Assert.That(ImportFileHelper.GetTableNameFromSelectStatement(sql), Is.EqualTo("[DATABASE].[SCHEMA].[TABLE]"));
        }

        [Test]
        public void GetTableFromQuery_ShouldReturnSqlResult_WhenNotSelectStatement()
        {
            const string sql = @"
DECLARE @sql nvarchar(max) = 'some dynamic sql'

EXEC sp_executesql (@sql)
";
            Assert.That(ImportFileHelper.GetTableNameFromSelectStatement(sql), Is.EqualTo("SQL RESULT"));
        }

    }
}
