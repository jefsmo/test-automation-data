# ConnectionStrings Class
- Create a static class to generate SQL Connection strings.  
- Use the **`connectionStrings`** section of the App.config file 
- Call the connection string using **`ConnectionStrings.WideWorldImportersCnn`**

## Example ConnectionStrings.cs

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
        private static string TestRunSetting => ConfigurationManager.AppSettings["testRunSetting"] = "LOCAL" ? "TEST" : ConfigurationManager.AppSettings["testRunSetting"];

        private static string LocalHost => ConfigurationManager.ConnectionStrings[TestRunSetting + ".localhost"].ConnectionString;
        #endregion

        #region CONNECTION STRINGS

        public static string WideWorldImportersCnn => GetDbConnectionString(LocalHost, "WideWorldImporters");

        #endregion

        #region PRIVATE METHODS
        private static string GetDbConnectionString(string serverConnection, string db_name, int connectionTimeout = 15)
        {
            var cnn = new SqlConnectionStringBuilder(serverConnection)
            {
                InitialCatalog = db_name,
                IntegratedSecurity = true,
                ConnectionTimeout = connectionTimeout
            }.ToString();

            if (Regex.IsMatch(cnn, @"^([^=;]+=[^=;]+)(;[^=;]+=[^=;]+)*$"))
            {
                return cnn;
            }

            Console.WriteLine($"Config: TestRunSetting\t{TestRunSetting,-30}");
            Console.WriteLine($"Server Connection\t{serverConnection, -30}");
            Console.WriteLine($"DB Name:\t{db_name}, -30")

            throw new FormatException($"Invalid SQL Connection String: {cnn}");
        }
        #endregion
    }
}
```

## Example App.config

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
