using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleSqlUnitTesting
{
    public class SqlDatabaseUtils
    {
        public static void CreateDatabase(string databaseName)
        {
            Console.WriteLine("Creating {0} Database...", databaseName);
            using (var conn = new SqlConnection(CreateConnectionStringForLocalDb("Master")))
            {
                conn.Open();

                var cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = string.Format(@"
	IF NOT EXISTS(SELECT * FROM sys.databases WHERE name='{0}')
	BEGIN
	    DECLARE @FILENAME AS VARCHAR(255)

	    SET @FILENAME = CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{0}';

		EXEC ('CREATE DATABASE [{0}] ON PRIMARY 
		(NAME = [{0}], 
		FILENAME =''' + @FILENAME + ''', 
		SIZE = 25MB, 
		MAXSIZE = 50MB, 
		FILEGROWTH = 5MB )')
	END",
                    databaseName)
                };
                cmd.ExecuteNonQuery();
            }
        }

        private static string CreateConnectionStringForLocalDb(string databaseName)
        {
            return $@"Data Source=(LocalDb)\Projects;Initial Catalog={databaseName};Integrated Security=True";

        }

        public static void ExecuteTestDbScripts(string subfolder, string databaseName)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Deployment", subfolder);

            var dir = new DirectoryInfo(directory);

            var files = dir.GetFiles("*.sql", SearchOption.AllDirectories)
                .Where(x => !x.Name.StartsWith("DISABLED_"))
                .OrderBy(x => x.FullName)
                .ToList();

            Debug.WriteLine("Deploying scripts on " + directory + "...");

            foreach (FileInfo sqlFile in files)
            {
                ExecuteSqlFile(sqlFile, databaseName);
            }
        }

        private static readonly Regex GoRegex = new Regex(@"\bGO\b", RegexOptions.Compiled);

        public static void ExecuteSqlFile(FileInfo sqlFile, string databaseName)
        {
            Console.WriteLine("Executing {0}", sqlFile);
            string sql = File.ReadAllText(sqlFile.FullName);
            ExecuteBatchedSql(sqlFile, databaseName, sql);
        }

        private static void ExecuteBatchedSql(FileInfo sqlFile, string databaseName, string sql)
        {
            string[] goSplitSql = GoRegex.Split(sql).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            using (var cnx = new SqlConnection(CreateConnectionStringForLocalDb(databaseName)))
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
                        Console.WriteLine("SQL EXCEPTION: " + ex.Message);
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

        public static void ExecuteSqlWithoutTransaction(string sql, string databaseName)
        {
            Console.WriteLine("Executing {0}", sql);

            using (var cnx = new SqlConnection(CreateConnectionStringForLocalDb(databaseName)))
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

