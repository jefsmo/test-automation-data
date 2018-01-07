# ConnectionStrings Class
Create a static class to generate SQL Connection strings.  
You can use the `connectionStrings` section of the App.config file or hard code the strings in the class.
Call the connection string using `ConnectionStrings.WideWorldImportersCnn`

## Example ConnectionStrings.cs
```
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Test.Automation.Data
{
    public static class ConnectionStrings
    {
        #region APP CONFIG SETTINGS
        private static string TestEnvironment
            => ConfigurationManager.AppSettings["testRunSetting"];

        private static string ServerConnection
            => ConfigurationManager.ConnectionStrings[TestEnvironment].ConnectionString;
        #endregion

        public static string WideWorldImportersCnn => GetDbConnectionString(ServerConnection, "WideWorldImporters");

        #region PRIVATE METHODS
        private static string GetDbConnectionString(string serverConnection, string db_name)
        {
            var cnn = new SqlConnectionStringBuilder(serverConnection)
            {
                InitialCatalog = db_name,
                IntegratedSecurity = true
            }.ToString();

            if (Regex.IsMatch(cnn, @"^([^=;]+=[^=;]+)(;[^=;]+=[^=;]+)*$"))
            {
                return cnn;
            }
            Console.WriteLine($"Config:TestEnvironment\t{TestEnvironment,-30}");
            Console.WriteLine($"Config:SqlCnnString\t{ServerConnection}");
            throw new FormatException($"Invalid SQL Connection String: {cnn}");
        }
        #endregion
    }
}
```

## Example App.config
```
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
    <add name="LOCAL" providerName="System.Data.SqlClient" connectionString="Data Source=localhost;" />
    <add name="TEST" providerName="System.Data.SqlClient" connectionString="Data Source=localhost;" />
  </connectionStrings>

</configuration>
```
