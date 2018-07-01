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
            Console.WriteLine($"Server Connection String\t{serverConnectionString,-30}");
            Console.WriteLine($"DB Name:\t{db_name}, -30");

            throw new FormatException($"Invalid SQL Connection String: {cnn}");
        }
        #endregion
    }
}
