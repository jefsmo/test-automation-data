using System;
using System.Data;
using NUnit.Framework;

namespace Test.Automation.Data.Tests
{
    [TestFixture]
    public class DataTableHelperTests
    {
        [Test]
        public void CompareDataTables_ReturnsDiffs_WhenTablesAreDifferent()
        {
            var expected = Common.CreateDataTable("Expected", new[] { 0 });
            Common.AddDataRow(expected, "my string", 42, 2d, 2.00m);
            expected.PrintDataTable();

            var actual = Common.CreateDataTable("Actual", expected.PrimaryKey);
            Common.AddDataRow(actual, "not my string", 99, 3d, 3.00m);
            actual.PrintDataTable();

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            diffs.PrintDataTable();
            Assert.That(diffs.Rows.Count, Is.EqualTo(5));
        }

        [Test]
        public void CompareDataTables_ReturnsEmpty_WhenNoDiffs()
        {
            var expected = Common.CreateDataTable("Expected", new[] { 0 });
            Common.AddDataRow(expected, "my string", 42, 2d, 2.00m);
            var actual = Common.CreateDataTable("Actual", expected.PrimaryKey);
            Common.AddDataRow(actual, "my string", 42, 2d, 2.00m);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            diffs.PrintDataTable();
            Assert.That(diffs.Rows.Count, Is.EqualTo(0));
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

    }
}
