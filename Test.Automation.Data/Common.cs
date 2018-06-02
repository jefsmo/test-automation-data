using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Test.Automation.Data
{
    /// <summary>
    /// Represents common methods used for database BVT testing.
    /// </summary>
    public static class Common
    {
        #region USER ROLES AND PERMISSIONS CHECKS

        /// <summary>
        /// Returns 1 if the user exists, else 0.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name"></param>
        /// <returns>Returns 1 if user exists, else 0.</returns>
        public static int GetUsernameCount(string connectionString, string user_name)
        {
            var paramUsername = new SqlParameter("@user_name", user_name.Trim());

            const string sql = @"
-- DECLARE @user_name sysname = 'DOMAIN\USER'

SELECT COUNT(*)
FROM [sys].[database_principals]
WHERE [name] = @user_name ;
";
            return (int)SqlHelper.ExecuteScalar(connectionString, sql, paramUsername);
        }

        /// <summary>
        /// Creates a user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name">The user to create.</param>
        public static void CreateUser(string connectionString, string user_name)
        {
            SqlServerVersionCheck(connectionString);

            var paramUsername = new SqlParameter("@user_name", user_name.Trim());

            const string sql = @"
-- DECLARE @user_name sysname = 'DOMAIN\USER'

DECLARE @createUser nvarchar(MAX) = 'CREATE USER [' + @user_name + '] FOR LOGIN [' + @user_name + '] WITH DEFAULT_SCHEMA = [dbo] ; '
PRINT @createUser ;

IF NOT EXISTS
(
	SELECT * 
	FROM [sys].[database_principals]
	WHERE [name] = @user_name
)
BEGIN
	EXEC (@createUser) ;
END
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramUsername);
        }

        /// <summary>
        /// Drops the user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name">The user to drop.</param>
        public static void DropUser(string connectionString, string user_name)
        {
            SqlServerVersionCheck(connectionString);

            var paramUsername = new SqlParameter("@user_name", user_name.Trim());

            const string sql = @"
-- DECLARE @user_name sysname = 'DOMAIN\USER'

DECLARE @dropUser nvarchar(MAX) = 'DROP USER [' + @user_name + '] ; '
PRINT @dropUser ;

IF EXISTS
(
	SELECT * 
	FROM [sys].[database_principals]
	WHERE [name] = @user_name
)
BEGIN
	EXEC (@dropUser) ;
END
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramUsername);
        }

        /// <summary>
        /// Returns a comma separated string of the DB roles belonging to the user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name">The user to get the roles for.</param>
        /// <returns>Returns comma separated list of roles.</returns>
        public static string GetRolesForUsername(string connectionString, string user_name)
        {
            var paramUsername = new SqlParameter("@user_name", user_name.Trim());

            const string sql = @"
-- DECLARE @user_name sysname = 'DOMAIN\USER'

SELECT r.[name] as [Role]
FROM [sys].[database_role_members] AS m
	INNER JOIN [sys].[database_principals] AS r	
		ON r.[principal_id] = m.[role_principal_id]
	INNER JOIN [sys].[database_principals] AS u 
		ON u.[principal_id] = m.[member_principal_id]
WHERE u.[name] = @user_name
ORDER BY [Role] ;
";
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, paramUsername);
            if(Debugger.IsAttached)
            {
                PrintTable(table);
            }
            if(table.Rows.Count > 0)
            {
                var colIdx = table.Columns["Role"].Ordinal;
                return table.Rows
                    .Cast<DataRow>()
                    .Select(dr => dr[colIdx].ToString())
                    .Aggregate((x, next) => x + ", " + next);
            }
            return null;
        }

        /// <summary>
        /// Adds the user to a DB role.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name">The user to add to the role.</param>
        /// <param name="role">The DB role being modified.</param>
        public static void AlterRoleAddMember(string connectionString, string user_name, string role)
        {
            SqlServerVersionCheck(connectionString);

            var paramUsername = new SqlParameter("@user_name", user_name.Trim());
            var paramRole = new SqlParameter("@role", role.Trim());

            const string sql = @"
--DECLARE @user_name sysname = 'DOMAIN\USER'
--DECLARE @role sysname = 'db_datareader'

DECLARE @alter_role_add_member nvarchar(MAX) = 'ALTER ROLE [' + @role + '] ADD MEMEBER [' + @user_name + '] ;'
PRINT @alter_role_add_member ;

IF NOT EXISTS
(
    SELECT
        dRole.name AS [Database Role Name]
        ,dPrinc.name AS [Members]
    FROM sys.database_role_members AS dRo
        JOIN sys.database_principals AS dPrinc
            ON dRo.member_principal_id = dPrinc.principal_id
        JOIN sys.database_principals AS dRole
            ON dRo.role_principal_id = dRole.principal_Id
    WHERE dPrinc.name = @user_name
        AND dRole.name = @role
)
BEGIN
    PRINT 'DB Role [' + @role + '] does not exist for user [' + @user_name + ']'
    EXEC (@alter_role_add_member)
    PRINT 'Member added to role'
END
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramUsername, paramRole);
        }

        /// <summary>
        /// Drops the user from a DB role.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="user_name">The user to drop from the role.</param>
        /// <param name="role">The DB role being modified.</param>
        public static void AlterRoleDropMember(string connectionString, string user_name, string role)
        {
            SqlServerVersionCheck(connectionString);

            var paramUsername = new SqlParameter("@user_name", user_name.Trim());
            var paramRole = new SqlParameter("@role", role.Trim());

            const string sql = @"
--DECLARE @user_name sysname = 'DOMAIN\USER'
--DECLARE @role sysname = 'db_datareader'

DECLARE @alter_role_drop_member nvarchar(MAX) = 'ALTER ROLE [' + @role + '] ADD MEMEBER [' + @user_name + '] ;'
PRINT @alter_role_drop_member ;

IF EXISTS
(
    SELECT
        dRole.name AS [Database Role Name]
        ,dPrinc.name AS [Members]
    FROM sys.database_role_members AS dRo
        JOIN sys.database_principals AS dPrinc
            ON dRo.member_principal_id = dPrinc.principal_id
        JOIN sys.database_principals AS dRole
            ON dRo.role_principal_id = dRole.principal_Id
    WHERE dPrinc.name = @user_name
        AND dRole.name = @role
)
BEGIN
    PRINT 'DB Role [' + @role + '] exists for user [' + @user_name + ']'
    EXEC (@alter_role_drop_member)
    PRINT 'Member dropped from role'
END
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramUsername, paramRole);
        }

        /// <summary>
        /// Returns a comma separated string of the fixed server roles belonging to the login.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login">The login to get the roles for.</param>
        /// <returns>Returns comma separated list of fixed server roles.</returns>
        public static string GetServerRolesForLogin(string connectionString, string login)
        {
            var paramLogin = new SqlParameter("@login", login.Trim());

            const string sql = @"
-- DECLARE @login sysname = 'DOMAIN\USER'

SELECT 
	SP.name AS [Server_Role]
FROM [sys].[server_role_members] AS SRM  
	JOIN [sys].[server_principals] AS SP  
		ON SRM.[Role_principal_id] = SP.[principal_id]
	JOIN [sys].[server_principals] AS SP2   
		ON SRM.[member_principal_id] = SP2.[principal_id]  
WHERE SP2.name = @login
ORDER BY  SP.[name], SP2.[name] ;
";
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, paramLogin);
            if(Debugger.IsAttached)
            {
                PrintTable(table);
            }
            if(table.Rows.Count > 0)
            {
                var colIdx = table.Columns["Server_Role"].Ordinal;
                return table.Rows
                    .Cast<DataRow>()
                    .Select(dr => dr[colIdx].ToString())
                    .Aggregate((x, next) => x + ", " + next);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login">The login to add to the server role.</param>
        /// <param name="server_role">The server role being modified.</param>
        public static void AlterServerRoleAddMember(string connectionString, string login, string server_role)
        {
            SqlServerVersionCheck(connectionString);

            var paramLogin = new SqlParameter("@login", login.Trim());
            var paramServerRole = new SqlParameter("server_role", server_role.Trim());

            const string sql = @"
-- DECLARE @login sysname = 'DOMAIN\USER'
-- DECLARE @server_role sysname = 'sysadmin'

DECLARE @alter_server_role_add_memmber nvarchar(MAX) = 'ALTER SERVER ROLE [' + @server_role + '] ADD MEMBER [' + @login + '] ;'
PRINT @alter_server_role_add_memmber ;

IF IS_SRVROLEMEMBER (@server_role, @login) = 1  
   print @login + '''s login is a member of the ' + @server_role + ' role' ;
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) = 0  
BEGIN
   print @login + '''s login is NOT a member of the ' +  @server_role + ' role' ;
   EXEC (@alter_server_role_add_memmber) ;
END
ELSE IF IS_SRVROLEMEMBER (@server_role)), @login) IS NULL  
   print 'ERROR: Invalid server role / login specified: ' + @server_role + ' / ' + @login ;  
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramLogin, paramServerRole);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login">The login to drop from the server role.</param>
        /// <param name="server_role">The server role being modified</param>
        public static void AlterServerRoleDropMember(string connectionString, string login, string server_role)
        {
            SqlServerVersionCheck(connectionString);

            var paramLogin = new SqlParameter("@login", login.Trim());
            var paramServerRole = new SqlParameter("server_role", server_role.Trim());

            const string sql = @"
-- DECLARE @login sysname = 'DOMAIN\USER'
-- DECLARE @server_role sysname = 'sysadmin'

DECLARE @alter_server_role_drop_member nvarchar(MAX) = 'ALTER SERVER ROLE [' + @server_role + '] DROP MEMBER [' + @login + '] ;'
PRINT @alter_server_role_drop_member ;

IF IS_SRVROLEMEMBER (@server_role, @login) = 1  
BEGIN
   print @login + '''s login is a member of the ' + @server_role + ' role' ;
   EXEC (@alter_server_role_drop_member) ;
END
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) = 0  
   print @login + '''s login is NOT a member of the ' +  @server_role + ' role' ;
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) IS NULL  
   print 'ERROR: Invalid server role / login specified: ' + @server_role + ' / ' + @login ;  
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, paramLogin, paramServerRole);
        }

        #endregion

        #region DB SCHEMA CHECKS

        /// <summary>
        /// Returns 1 if the database exists, else 0.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="db_name">The database name.</param>
        /// <returns>Returns 1 if DB exists, else 0.</returns>
        public static int GetDatabaseNameCount(string connectionString, string db_name)
        {
            var paramName = new SqlParameter("@db_name", db_name.Trim());

            const string sql = @"
-- DECLARE @db_name sysname = 'master'

SELECT COUNT(*)
FROM [sys].[databases]
WHERE [name] = @db_name) ; 
";
            return (int)SqlHelper.ExecuteScalar(connectionString, sql, paramName);
        }

        /// <summary>
        /// Gets a list of DB tables.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of tables.</returns>
        public static IEnumerable<string> GetTables(string connectionString)
        {
            const string sql = @"
SELECT
	s.[name] + '.' + t.[name] as [Name]
FROM [sys].[tables] t
	JOIN [sys].[schemas] s
		ON s.schema_id = t.schema_id
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of DB views.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of views.</returns>
        public static IEnumerable<string> GetViews(string connectionString)
        {
            const string sql = @"
SELECT
	s.[name] + '.' + v.[name] as [Name]
FROM [sys].[views] v
	JOIN [sys].[schemas] s
		ON s.schema_id = v.schema_id
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of DB stored procedures.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of stored procedures.</returns>
        public static IEnumerable<string> GetStoredProcedures(string connectionString)
        {
            const string sql = @"
SELECT
	s.[name] + '.' + p.[name] as [Name]
FROM [sys].[procedures] p
	JOIN [sys].[schemas] s
		ON s.schema_id = p.[schema_id]
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of DB Scalar Functions.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of scalar functions.</returns>
        public static IEnumerable<string> GetScalarFunctions(string connectionString)
        {
            const string sql = @"
SELECT 
	s.[name] + '.' + o.[name] as [Name]
FROM [sys].[objects] o
	JOIN [sys].[schemas] s 
		ON s.[schema_id] = o.[schema_id]
WHERE [type] = 'FN'
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of DB Table Functions.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of table functions.</returns>
        public static IEnumerable<string> GetTableFunctions(string connectionString)
        {
            const string sql = @"
SELECT 
	s.[name] + '.' + o.[name] as [Name]
FROM [sys].[objects] o
	JOIN [sys].[schemas] s 
		ON s.[schema_id] = o.[schema_id]
WHERE [type] = 'IF'
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of DB User-Defined Data Types.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of user-defined data types.</returns>
        public static IEnumerable<string> GetUserDefinedDataTypes(string connectionString)
        {
            const string sql = @"
SELECT 
	s.[name] + '.' + t.[name] as [Name]
FROM [sys].[types] t
	JOIN [sys].[schemas] s 
		ON s.[schema_id] = t.[schema_id]
WHERE t.is_user_defined = 1 AND is_table_type = 0
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        /// <summary>
        /// Gets a list of User-Defined Table Types.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of user-defined table types.</returns>
        public static IEnumerable<string> GetUserDefinedTableTypes(string connectionString)
        {
            const string sql = @"
SELECT 
	s.[name] + '.' + tt.[name] as [Name]
FROM [sys].[types] tt
	JOIN [sys].[schemas] s 
		ON s.[schema_id] = tt.[schema_id]
WHERE t.is_user_defined = 1 AND is_table_type = 1
ORDER BY [Name] ; 
";
            return GetSchemaList(connectionString, sql);
        }

        #endregion

        #region UTILITY METHODS

        /// <summary>
        /// Verifies the actual data matches the expected data.
        /// </summary>
        /// <param name="expected">The expected values.</param>
        /// <param name="actual">The actual values.</param>
        public static void Verify(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.Count(), Is.EqualTo(expected.Count()));
                Assert.That(actual, Is.EquivalentTo(expected));
            });
        }

        /// <summary>
        /// Prints the SQL result as a table, optionally using quotes.
        /// Copy/paste the quoted result into your expected data values.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="isWithQuotes"></param>
        public static void PrintTable(DataTable table, bool isWithQuotes = false)
        {
            Console.WriteLine(table.TableName.ToUpperInvariant());

            foreach(DataColumn col in table.Columns)
            {
                Console.WriteLine($"{col.ColumnName, -25}");
            }

            foreach(DataRow row in table.Rows)
            {
                foreach(DataColumn col in table.Columns)
                {
                    if (isWithQuotes)
                    {
                        Console.WriteLine($"\"{row[col]}\",");
                    }
                    else
                    {
                        Console.WriteLine($"{row[col],-25}");
                    }
                }
            }
            Console.WriteLine();
        }

        #endregion

        #region PRIVATE METHODS

        private static IEnumerable<string> GetSchemaList(string connectionString, string sql)
        {
            var table = SqlHelper.ExecuteDataTable(connectionString, sql);
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            var colIdx = table.Columns["Name"].Ordinal;
            return table.Rows.Cast<DataRow>().Select(dr => (string)dr[colIdx]);
        }

        private static void SqlServerVersionCheck(string serverCnn)
        {
            // Returns: major.minor.build.revision
            var productVersion = SqlHelper.ExecuteScalar(serverCnn, "SELECT SERVERPROPERTY('ProductVersion') AS ProductVersion") as string;
            var major = Convert.ToInt32(productVersion.Split('.')[0]);

            if (major < 10)
            {
                throw new ApplicationException($"This method contains syntax that is not supported by SQL Server version '{productVersion}'.\r\n" +
                    $"Create a custom query using supported syntax instead.");
            }
        }

        #endregion
    }
}

