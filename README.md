# Test.Automation.Data

**README.md**

## Test.Automation.Data
This project creates a NuGet package containing a static SqlHelper class that tests can use to retrieve data from a SQL database.  

SqlHelper:
- handles all the connection, command, and reader resources automatically
- executes an SQL statement or stored procedure 
- logs connection, command, parameter, and result when run in debug mode
- commands are executed using one of the methods:
  - ExecuteDataTable
  - ExecuteReader
  - ExecuteScalar
  - ExecuteNonQuery

Install the NuGet package into your database test class.

In addition, there is a Common class that contains common SQL queries used for database BVT tests.
This includes users, roles and schema objects.

## Example Database Test
~~~csharp
using System;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Automation.Data;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var paramCustomerName = new SqlParameter("@customerName", "Wingtip Toys (Caro, MI)");

            const string sql = @"
--DECLARE @customerName nvarchar(100) = 'Wingtip Toys (Caro, MI)'

SELECT COUNT(*)
FROM [WideWorldImporters].[Sales].[Customers]
WHERE [CustomerName] = @customerName
";

            var actual = (int)SqlHelper.ExecuteScalar(ConnectionStrings.WideWorldImportersCnn, sql, paramCustomerName);

            Assert.AreEqual(1, actual);
        }
    }
}

~~~

### Debug Mode Output
~~~text
Test Name:	TestMethod1
Test Outcome:	Passed
Result StandardOutput:	
ConnectionString         	Data Source=localhost;Initial Catalog=WideWorldImporters;Integrated Security=True;Connect Timeout=15
SQL Query                	
--DECLARE @customerName nvarchar(100) = 'Wingtip Toys (Caro, MI)'

SELECT COUNT(*)
FROM [WideWorldImporters].[Sales].[Customers]
WHERE [CustomerName] = @customerName

SQL Result               	1                        
SQL Parameters           	| Input - @customerName : Wingtip Toys (Caro, MI) |


~~~

## ConnectionStrings Class
- Create a static class to generate SQL Connection strings.  
- Use the **`connectionStrings`** section of the App.config file 
- Call the connection string using **`ConnectionStrings.WideWorldImportersCnn`**

### Example ConnectionStrings.cs

```csharp
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Test.Automation.Data
{
    public static class ConnectionStrings
    {
        #region APP CONFIG SETTINGS
        private static string TestRunSetting => ConfigurationManager.AppSettings["testRunSetting"] == "LOCAL"
            ? "TEST"
            : ConfigurationManager.AppSettings["testRunSetting"];

        private static string LocalHost => ConfigurationManager.ConnectionStrings[TestRunSetting + ".localhost"].ConnectionString;
        #endregion

        #region CONNECTION STRINGS

        public static string WideWorldImportersCnn => GetDbConnectionString(LocalHost, "WideWorldImporters");

        #endregion

        #region PRIVATE METHODS
        private static string GetDbConnectionString(string serverConnectionString, string db_name, int connectTimeout = 15)
        {
            var cnn = new SqlConnectionStringBuilder(serverConnectionString)
            {
                InitialCatalog = db_name,
                IntegratedSecurity = true,
                ConnectTimeout = connectTimeout
            }.ToString();

            if (Regex.IsMatch(cnn, @"^([^=;]+=[^=;]+)(;[^=;]+=[^=;]+)*$"))
            {
                return cnn;
            }

            Console.WriteLine($"Config: TestRunSetting\t{TestRunSetting,-30}");
            Console.WriteLine($"Server Connection String\t{serverConnectionString, -30}");
            Console.WriteLine($"DB Name:\t{db_name}, -30");

            throw new FormatException($"Invalid SQL Connection String: {cnn}");
        }
        #endregion
    }
}
```

### Example App.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>

  <appSettings>
    <!--
    testRunSetting = Choose a test environment. [ LOCAL | TEST | < ... > ]
    ===========================================================================
    -->
    <add key="testRunSetting" value="TEST" />
  </appSettings>

  <connectionStrings>
    <clear />
    <add name="TEST.localhost" providerName="System.Data.SqlClient" connectionString="Data Source=localhost;" />
  </connectionStrings>

</configuration>
```

### Schema.ini
~~~text
When the Text driver is used, the format of the text file is determined by using a schema information file.
The schema information file is always named Schema.ini and always kept in the same directory as the text data source.

A Schema.ini file is always required for accessing fixed-length data.
You should use a Schema.ini file when your text table contains DateTime, Currency, or Decimal data,
or any time that you want more control over the handling of the data in the table.
~~~

|Format Specifier|Table Format|
|---|---|
|TabDelimited|Fields in the file delimited by tabs.|
|CSVDelimited|Fields in the file delimited by commas.|
|Delimited(*)|Fields in the file delimited by * or other char.|
|FixedLength|Fields in the file are of fixed length.|


## Creating Packages Locally
### OctoPack Command Line Reference
#### Create a Local NuGet Package with OctoPack
- Add OctoPack NuGet package to each project in the solution that you want to package.
- Add a `.nuspec` file to each project in the solution that you want to package with NuGet.
- The `.nuspec` file name **must be the same name as the project** with the `.nuspec` extension
- Open a '`Developer Command Prompt for VS2017`' command window.
- Navigate to the solution or project that you want to OctoPack.
- Run the following command:

```text
// To Create packages for each project in the solution:
MSBUILD Test.Automation.Data.sln /t:Rebuild /p:Configuration=Release /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.0 /p:OctoPackPublishPackageToFileShare=C:\Packages

// To Create a package for a single project:
MSBUILD Test.Automation.Data.csproj /t:Rebuild /p:Configuration=Release /p:RunOctoPack=true /p:OctoPackPackageVersion=1.0.0 /p:OctoPackPublishPackageToFileShare=C:\Packages
```

#### MSBUILD OctoPack Command Syntax
|Switch|Value|Definition|
|-----|-----|-----|
|`/t:Rebuild`|  |MSBUILD Rebuilds the project(s).|
|`/p:Configuration=`|`Release`|Creates and packages a Release build.|
|`/p:RunOctoPack=`|`true`|Creates packages with Octopack using the .nuspec file layout.|
|`/p:OctoPackPackageVersion=`|`1.0.0`|Updates Package Version.|
|`/p:OctoPackPublishPackageToFileShare=`|`C:\Packages`|Copies packages to local file location.|

eof
