using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;

namespace Frontiers.Impact.ImpactDB.Tests
{
    [TestClass]
    public class SqlDatabaseSetup
    {
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext ctx)
        {
            CreateDatabase("AdventureWorks_UnitTesting");
            DeployImpactTestSchema();
            ExecuteTestDbScripts("01_AdventureWorks", "AdventureWorks_UnitTesting");
            ExecuteSqlWithoutTransaction("RECONFIGURE", "AdventureWorks_UnitTesting");
        }

        private static void DeployImpactTestSchema()
        {
            Debug.WriteLine("Deploying Database Project...");
            SqlDatabaseTestClass.TestService.DeployDatabaseProject();
            // This can only work if you have VS Ultimate
            // SqlDatabaseTestClass.TestService.GenerateData();
        }

        private static void CreateDatabase(string databaseName)
        {
			string masterConnectionString = CreateConnectionStringForDatabase("Master_DB");

            Console.WriteLine("Creating {0} Database...", databaseName);
			Console.WriteLine("ConnectionString for MasterDatabase={0}", masterConnectionString);

			using (var conn = new SqlConnection(masterConnectionString))
            {
                conn.Open();

                var cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = string.Format(@"
						IF EXISTS(SELECT * FROM sys.databases WHERE name='{0}')
						BEGIN
							USE master;
							ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
							DROP DATABASE [{0}];
						END

						DECLARE @FILENAME AS VARCHAR(255)

						SET @FILENAME = CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{0}';

						EXEC ('CREATE DATABASE [{0}] ON PRIMARY 
						(NAME = [{0}], 
						FILENAME =''' + @FILENAME + ''', 
						SIZE = 25MB, 
						MAXSIZE = 50MB, 
						FILEGROWTH = 5MB )')",
                    databaseName)
                };
                cmd.ExecuteNonQuery();
            }
        }

        private static string CreateConnectionStringForDatabase(string databaseName)
        {
			return ConfigurationManager.ConnectionStrings[databaseName].ConnectionString;
        }

        private static void ExecuteTestDbScripts(string subfolder, string databaseName)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Deployment", subfolder);

            var dir = new DirectoryInfo(directory);

            var files = dir.GetFiles("*.sql", SearchOption.AllDirectories)
                .Where(x => !x.Name.StartsWith("DISABLED_"))
                .OrderBy(x => x.FullName)
                .ToList();
#if IMPCIMAD4
            files = files.Where(x => !x.Name.Contains("_LOCAL_")).ToList();
#else
            files = files.Where(x => !x.Name.Contains("_IMPCIMAD4_")).ToList();
#endif
            Debug.WriteLine("Deploying scripts on " + directory + "...");

            foreach (FileInfo sqlFile in files)
            {
                ExecuteSqlFile(sqlFile, databaseName);
            }
        }

        private static readonly Regex GoRegex = new Regex(@"\bGO\b", RegexOptions.Compiled);

        private static void ExecuteSqlFile(FileInfo sqlFile, string databaseName)
        {
            Debug.WriteLine("Executing {0}", sqlFile);
            string sql = File.ReadAllText(sqlFile.FullName);
            ExecuteBatchedSql(sqlFile, databaseName, sql);
        }

        private static void ExecuteBatchedSql(FileInfo sqlFile, string databaseName, string sql)
        {
            string[] goSplitSql = GoRegex.Split(sql).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            using (var cnx = new SqlConnection(CreateConnectionStringForDatabase(databaseName)))
            {
                cnx.Open();
                using (var trn = cnx.BeginTransaction("InitSqlPart"))
                {
                    try
                    {
                        foreach (var sqlPart in goSplitSql)
                        {
                            using (var cmd = new SqlCommand(sqlPart, cnx, trn))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }

                        trn.Commit();
                    }
                    catch (SqlException ex)
                    {
                        Debug.WriteLine("SQL EXCEPTION: " + ex.Message);
                        try
                        {
                            trn.Rollback();
                        }
                        catch (SqlException rollBackEx)
                        {
                            Console.WriteLine("SQL EXCEPTION ON ROLLBACK: " + rollBackEx.Message);
                        }

                        throw new Exception(
                            "Failed executing " + sqlFile + ". You can disable this file by prepending DISABLED_ to its name.",
                            ex);
                    }
                }
            }
        }

        private static void ExecuteSqlWithoutTransaction(string sql, string databaseName)
        {
            Console.WriteLine("Executing {0}", sql);

            using (var cnx = new SqlConnection(CreateConnectionStringForDatabase(databaseName)))
            {
                cnx.Open();
                try
                {
                    using (var cmd = new SqlCommand(sql, cnx))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("SQL EXCEPTION: " + ex.Message);
                    throw new Exception("Failed executing SQL.", ex);
                }
            }
        }
    }
}

