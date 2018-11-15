using System.IO;
using NUnit.Framework;

namespace Test.Automation.Data.Tests
{
    [TestFixture]
    public class FileImportHelperTests
    {
        [Test]
        public void ExecuteDataTableFromTextFile_ShouldImport_WhenCsv()
        {
            const string query = @"SELECT * FROM [Test#csv]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "Test.csv");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00d, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00d, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00d, 4.00m);
            Common.AddDataRow(expected, "Text four.", 43, 5.00d, 5.00m);

            var actual = ImportFileHelper.ExecuteDataTableFromTextFile(query, file, expected.PrimaryKey);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            Assert.That(diffs, Is.Null);
        }

        [Test]
        public void ExecuteDataTableFromTextFile_ShouldImport_WhenTabDelimited()
        {
            const string query = @"SELECT * FROM [Test#tab]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "Test.tab");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00, 4.00m);
            Common.AddDataRow(expected, "Text four.", 43, 5.00, 5.00m);

            var actual = ImportFileHelper.ExecuteDataTableFromTextFile(query, file, expected.PrimaryKey);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            Assert.That(diffs, Is.Null);
        }

        [Test]
        public void ExecuteDataTableFromExcel_ShouldImport_WhenExcel()
        {
            const string query = @"SELECT * FROM [Sheet1$]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "Test.xlsx");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0, 2 });
            Common.AddDataRow(expected, "Text one.", 42, 2.00, 2.00m);
            Common.AddDataRow(expected, "Text two.", 42, 3.00, 3.00m);
            Common.AddDataRow(expected, "Text three.", 43, 4.00, 4.00m);
            Common.AddDataRow(expected, "Text four.", 43, 5.00, 5.00m);

            var actual = ImportFileHelper.ExecuteDataTableFromExcel(query, file, expected.PrimaryKey);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            Assert.That(diffs, Is.Null);
        }

    }
}
