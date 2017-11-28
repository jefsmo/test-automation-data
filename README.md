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

namespace UnitTestProject1
{
    public static class ConnectionStrings
    {
        private static string TestEnvironment
            => ConfigurationManager.AppSettings["testRunSetting"];

        private static string SqlCnnString
            => ConfigurationManager.ConnectionStrings[TestEnvironment].ConnectionString;

        public static string WideWorldImportersCnn => GetDbConnectionString("WideWorldImporters");

        private static string GetDbConnectionString(string db_name)
        {
            var cnn =  new SqlConnectionStringBuilder(SqlCnnString)
            {
                InitialCatalog = db_name,
                IntegratedSecurity = true
            }.ToString();

            if(Regex.IsMatch(cnn, @"^([^=;]+=[^=;]+)(;[^=;]+=[^=;]+)*$"))
            {
                return cnn;
            }
            Console.WriteLine($"Config:TestEnvironment\t{TestEnvironment,-30}");
            Console.WriteLine($"Config:SqlCnnString\t{SqlCnnString}");
            throw new FormatException($"Invalid SQL Connection String: {cnn}");
        }
    }
}
```

## Example App.config
```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>

  <appSettings>
    <add key="testRunSetting" value="LOCAL"/>
  </appSettings>

  <connectionStrings>
    <clear/>
    <add name="LOCAL" connectionString="Data Source=localhost"/>
    <add name="TEST" connectionString="Data Source=localhost"/>
  </connectionStrings>

</configuration>
```
