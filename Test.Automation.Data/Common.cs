using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Shouldly;

namespace Test.Automation.Data
{
    /// <summary>
    /// Represents common methods used for database BVT testing.
    /// </summary>
    public static class Common
    {
        #region USER ROLES AND PERMISSIONS

        /// <summary>
        /// Returns 1 if the user exists, else 0.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username"></param>
        /// <returns></returns>
        public static int GetUsernameCount(string connectionString, string username)
        {
            var paramUsername = new SqlParameter("@user_name", username);

            const string sql = @"
SELECT COUNT(*)
FROM [sys].[database_principals]
WHERE [name] = @user_name ;
";
            return (int)SqlHelper.ExecuteScalar(connectionString, sql, CommandType.Text, paramUsername);
        }

        /// <summary>
        /// Creates a user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username"></param>
        public static void CreateUser(string connectionString, string username)
        {
            var paramUsername = new SqlParameter("@user_name", username);

            const string sql = @"
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
            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramUsername);

        }

        /// <summary>
        /// Drops the user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username"></param>
        public static void DropUser(string connectionString, string username)
        {
            var paramUsername = new SqlParameter("@user_name", username);

            const string sql = @"
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
            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramUsername);
        }

        /// <summary>
        /// Returns a comma separated string of the DB roles belonging to the user.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username">The user to get the roles for.</param>
        /// <returns></returns>
        public static string GetRolesForUsername(string connectionString, string username)
        {
            var paramUsername = new SqlParameter("@user_name", username);

            const string sql = @"
SELECT r.[name] as [Role]
FROM [sys].[database_role_members] AS m
	INNER JOIN [sys].[database_principals] AS r	
		ON r.[principal_id] = m.[role_principal_id]
	INNER JOIN [sys].[database_principals] AS u 
		ON u.[principal_id] = m.[member_principal_id]
WHERE u.[name] = @user_name
ORDER BY [Role] ;
";
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, paramUsername);
            if(Debugger.IsAttached)
            {
                PrintTable(table);
            }
            if(table.Rows.Count > 0)
            {
                return table.Rows.Cast<string>().Aggregate((x, next) => x + ", " + next);
            }
            return null;
        }

        /// <summary>
        /// Adds the user to a DB role.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username">The user to add to the role.</param>
        /// <param name="role">The DB role being modified.</param>
        public static void AlterRoleAddMember(string connectionString, string username, string role)
        {
            var paramUsername = new SqlParameter("@user_name", username);
            var paramRole = new SqlParameter("@role", role);

            const string sql = @"
DECLARE @alter_role_add_member nvarchar(MAX) = 'ALTER ROLE [' + @role + '] ADD MEMEBER [' + @user_name + '] ;'
PRINT @alter_role_add_member ;

 IF IS_ROLEMEMBER (@role, @user_name) = 1  
   print @user_name + ' is a member of the ' + @role + ' role'  
ELSE IF IS_ROLEMEMBER (@role, @user_name) = 0  
BEGIN
   print  ' is NOT a member of the ' + @role + ' role'  
   EXEC (@alter_role_add_member)
END
ELSE IF IS_ROLEMEMBER (@role, @user_name) IS NULL  
   print 'ERROR: Invalid database role / database_principal specified: ' + @role + ' / ' + @user_name ;  
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramUsername, paramRole);
        }

        /// <summary>
        /// Drops the user from a DB role.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="username">The user to drop from the role.</param>
        /// <param name="role">The DB role being modified.</param>
        public static void AlterRoleDropMember(string connectionString, string username, string role)
        {
            var paramUsername = new SqlParameter("@user_name", username);
            var paramRole = new SqlParameter("@role", role);

            const string sql = @"
DECLARE @alter_role_drop_member nvarchar(MAX) = 'ALTER ROLE [' + @role + '] DROP MEMEBER [' + @user_name + '] ;'
PRINT @alter_role_drop_member ;

 IF IS_ROLEMEMBER (@role, @user_name) = 1  
 BEGIN
   print @user_name + ' is a member of the ' + @role + ' role'  
   EXEC (@alter_role_drop_member)
END
ELSE IF IS_ROLEMEMBER (@role, @user_name) = 0  
   print  ' is NOT a member of the ' + @role + ' role'  
ELSE IF IS_ROLEMEMBER (@role, @user_name) IS NULL  
   print 'ERROR: Invalid database role / database_principal specified: ' + @role + ' / ' + @user_name ;  
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramUsername, paramRole);
        }

        /// <summary>
        /// Returns a comma separated string of the fixed server roles belonging to the login.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login"></param>
        /// <returns></returns>
        public static string GetServerRolesForLogin(string connectionString, string login)
        {
            var paramLogin = new SqlParameter("@login", login);

            const string sql = @"
SELECT 
	--SRM.role_principal_id
	SP.name AS Role_Name
	--, SRM.member_principal_id
	--, SP2.name  AS Member_Name  
FROM sys.server_role_members AS SRM  
	JOIN sys.server_principals AS SP  
		ON SRM.Role_principal_id = SP.principal_id  
	JOIN sys.server_principals AS SP2   
		ON SRM.member_principal_id = SP2.principal_id  
WHERE SP2.name = @login
ORDER BY  SP.name, SP2.name ;
";
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, paramLogin);
            if(Debugger.IsAttached)
            {
                PrintTable(table);
            }
            if(table.Rows.Count > 0)
            {
                return table.Rows.Cast<string>().Aggregate((x, next) => x + ", " + next);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login">The login to add to the server role.</param>
        /// <param name="serverrole">The server role being modified.</param>
        public static void AlterServerRoleAddMember(string connectionString, string login, string serverrole)
        {
            var paramLogin = new SqlParameter("@login", login);
            var paramServerRole = new SqlParameter("server_role", serverrole);

            const string sql = @"
DECLARE @alter_server_role_add_memmber nvarchar(MAX) = 'ALTER SERVER ROLE [' + @server_role + '] ADD MEMBER [' + @login + '] ;'
PRINT @alter_server_role_add_memmber ;


IF IS_SRVROLEMEMBER (@server_role, @login) = 1  
   print @login + '''s login is a member of the ' + @server_role + ' role'  
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) = 0  
BEGIN
   print @login + '''s login is NOT a member of the ' +  @server_role + ' role'  
   EXEC (@alter_server_role_add_memmber)
END
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) IS NULL  
   print 'ERROR: Invalid server role / login specified: ' + @server_role + ' / ' + @login ;  
";
            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramLogin, paramServerRole);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <param name="login">The login to drop from the server role.</param>
        /// <param name="serverrole">The server role being modified</param>
        public static void AlterServerRoleDropMember(string connectionString, string login, string serverrole)
        {
            var paramLogin = new SqlParameter("@login_name", login);
            var paramServerRole = new SqlParameter("server_role", serverrole);

            const string sql = @"
DECLARE @alter_server_role_drop_member nvarchar(MAX) = 'ALTER SERVER ROLE [' + @server_role + '] DROP MEMBER [' + @login + '] ;'
PRINT @alter_server_role_drop_member ;

IF IS_SRVROLEMEMBER (@server_role, @login) = 1  
BEGIN
   print @login + '''s login is a member of the ' + @server_role + ' role'  
   EXEC (@alter_server_role_drop_member)
END
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) = 0  
   print @login + '''s login is NOT a member of the ' +  @server_role + ' role'  
ELSE IF IS_SRVROLEMEMBER (@server_role, @login) IS NULL  
   print 'ERROR: Invalid server role / login specified: ' + @server_role + ' / ' + @login ;  
";

            SqlHelper.ExecuteNonQuery(connectionString, sql, CommandType.Text, paramLogin, paramServerRole);
        }

        #endregion

        #region DB SCHEMA

        /// <summary>
        /// Returns 1 if the database exists, else 0.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="dbname">The database name.</param>
        /// <returns></returns>
        public static int GetDatabaseNameCount(string connectionString, string dbname)
        {
            var paramDbName = new SqlParameter("@db_name", dbname);

            const string sql = @"
SELECT COUNT(*)
FROM [sys].[databases]
WHERE [name] = @db_name ; 
";
            return (int)SqlHelper.ExecuteScalar(connectionString, sql, CommandType.Text, paramDbName);
        }

        /// <summary>
        /// Gets a list of DB tables.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns>Returns a list of table names.</returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of DB views.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of DB stored procedures.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of DB Scalar Functions.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of DB Table Functions.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of DB User-Defined Data Types.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        /// <summary>
        /// Gets a list of User-Defined Table Types.
        /// </summary>
        /// <param name="connectionString">The DB connection string.</param>
        /// <returns></returns>
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
            var table = SqlHelper.ExecuteDataTable(connectionString, sql, CommandType.Text, default(SqlParameter));
            if (Debugger.IsAttached)
            {
                PrintTable(table, true);
            }
            return table.Rows.Cast<string>().ToList();
        }

        #endregion

        #region UTILITY

        /// <summary>
        /// Verifies the actual data matches the expected data.
        /// </summary>
        /// <param name="expected">The expected values.</param>
        /// <param name="actual">The actual values.</param>
        public static void Verify(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            actual.ShouldSatisfyAllConditions
                (
                    () => actual.Count().ShouldBe(expected.Count()),
                    () => actual.Except(expected).ShouldBe(Enumerable.Empty<string>(),
                    "Item is missing from expected test data. " +
                    "Run test in debug mode to generate expected data in output window."),
                    () => expected.Except(actual).ShouldBe(Enumerable.Empty<string>(),
                    "Item is missing from DB.")
                );
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
            Console.WriteLine();

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
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        #endregion
    }
}
