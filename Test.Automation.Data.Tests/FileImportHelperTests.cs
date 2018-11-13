using System.IO;
using NUnit.Framework;

namespace Test.Automation.Data.Tests
{
    [TestFixture]
    public class FileImportHelperTests
    {
        [Test]
        public void ExecuteDataTableFromTextFile_ShouldImportdata_WhenCsv()
        {
            const string query = @"SELECT * FROM [CsvData#csv]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "CsvData.csv");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0 });
            Common.AddDataRow(expected, "My string", 42, 300.5d, 300.5m);
            Common.AddDataRow(expected, "New string", 33, 0.00d, 0.00m);

            var actual = ImportFileHelper.ExecuteDataTableFromTextFile(query, file, expected.PrimaryKey);

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            diffs.PrintDataTable();
            Assert.That(diffs.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteDataTableFromTextFile_ShouldImportdata_WhenTabDelimitedTxt()
        {
            const string query = @"SELECT * FROM [TextDataTabDelimited#txt]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TextDataTabDelimited.txt");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0 });
            Common.AddDataRow(expected, "My string", 42, 300.5d, 300.5m);
            Common.AddDataRow(expected, "New string", 33, 0.00d, 0.00m);
            expected.PrintDataTable();

            var actual = ImportFileHelper.ExecuteDataTableFromTextFile(query, file, expected.PrimaryKey);
            actual.PrintDataTable();

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            diffs.PrintDataTable();
            Assert.That(diffs.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteDataTableFromTextFile_ShouldImportdata_WhenPipeDelimitedTxt()
        {
            const string query = @"SELECT * FROM [TextDataPipeDelimited#txt]";
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TextDataPipeDelimited.txt");
            var file = new FileInfo(path);

            var expected = Common.CreateDataTable("Expected", new[] { 0 });
            Common.AddDataRow(expected, "My string", 42, 300.5d, 300.5m);
            Common.AddDataRow(expected, "New string", 33, 0.00d, 0.00m);
            expected.PrintDataTable();

            var actual = ImportFileHelper.ExecuteDataTableFromTextFile(query, file, expected.PrimaryKey);
            actual.PrintDataTable();

            var diffs = DataTableHelper.CompareDataTables(expected, actual);
            diffs.PrintDataTable();
            Assert.That(diffs.Rows.Count, Is.EqualTo(0));
        }

    }
}
