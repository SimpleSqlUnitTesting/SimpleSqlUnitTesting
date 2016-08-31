using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting.Conditions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frontiers.Impact.ImpactDB.Tests.Framework
{
    internal static class ExecutionEngine2
    {
        internal static void EvaluateConditions(DbConnection validationConnection, SqlExecutionResult[] results, Collection<TestCondition> conditions)
        {
            int count = conditions.Count;
            for (int i = 0; i < count; i++)
            {
                TestCondition condition = conditions[i];
                if (condition == null)
                {
                    object[] args = { i };
                    throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "Condition at index {0} was null. Conditions cannot be null.", args));
                }
                if (condition.Enabled)
                {
                    PrintMessage("Validating {0}", condition.Name);
                    condition.Assert(validationConnection, results);
                }
            }
        }

        public static SqlExecutionResult[] ExecuteTest(ConnectionContext ctx, string testSql, params DbParameter[] tsqlParameters)
        {
            if (ctx == null)
            {
                throw new AssertFailedException("The ConnectionContext cannot be null");
            }
            if (ctx.Connection == null)
            {
                throw new AssertFailedException("The connection cannot be null");
            }
            if (ctx.Provider == null)
            {
                throw new AssertFailedException("The provider factory cannot be null");
            }
            if (testSql == null)
            {
                throw new AssertFailedException("The T-SQL string cannot be null");
            }
            AssertConnectionStateValid(ConnectionState.Open, ctx.Connection.State);
            DbCommand command = ctx.Connection.CreateCommand();
            SqlExecutionResult result = new SqlExecutionResult();
            int num = 0;
            int num2 = 0;
            int rowsAffectedCount = 0;
            List<int> rowsAffected = new List<int>();
            SqlCommand command2 = command as SqlCommand;
            SqlConnection connection = ctx.Connection as SqlConnection;
            SqlInfoMessageEventHandler handler = null;
            StatementCompletedEventHandler handler2 = null;
            if (connection != null)
            {
                handler = delegate (object sender, SqlInfoMessageEventArgs e) {
                                                                                  ProcessErrors(e.Errors);
                };
                connection.InfoMessage += handler;
            }
            if (command2 != null)
            {
                handler2 = delegate (object sender, StatementCompletedEventArgs e) {
                                                                                       rowsAffectedCount += e.RecordCount;
                                                                                       rowsAffected.Add(e.RecordCount);
                };
                command2.StatementCompleted += handler2;
            }
            try
            {
                DataSet dataSet = new DataSet
                {
                    Locale = CultureInfo.CurrentCulture
                };
                command.CommandText = testSql;
                command.CommandType = CommandType.Text;
                command.Transaction = ctx.Transaction;
                command.CommandTimeout = ctx.CommandTimeout;
                if (tsqlParameters != null)
                {
                    int length = tsqlParameters.Length;
                    for (int i = 0; i < length; i++)
                    {
                        DbParameter parameter = tsqlParameters[i];
                        command.Parameters.Add(parameter);
                    }
                }
                DbDataAdapter adapter1 = ctx.Provider.CreateDataAdapter();
                adapter1.SelectCommand = command;
                DateTime now = DateTime.Now;
                adapter1.Fill(dataSet);
                DateTime time2 = DateTime.Now;
                result.DataSet = dataSet;
                result.ExecutionTime = time2.Subtract(now);
                result.RowsAffected = rowsAffected.ToArray();
                num++;
                num2 += dataSet.Tables.Count;
            }
            catch (SqlException exception1)
            {
                ProcessErrors(exception1.Errors);
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    connection.InfoMessage -= handler;
                }
                if (command2 != null)
                {
                    command2.StatementCompleted -= handler2;
                }
            }
            PrintMessage("{0} batches, {1} ResultSets, {2} rows affected", num, num2, rowsAffectedCount);
            return new[] { result };
        }
        public static void AssertConnectionStateValid(ConnectionState expected, ConnectionState actual)
        {
            Assert.AreEqual(expected, actual, "The connection state is {0}. The state is expected to be {1}.", actual, expected);
        }

        internal static void PrintMessage(string message)
        {
            Trace.WriteLine(message);
        }

        [StringFormatMethod("format")]
        internal static void PrintMessage(string format, params object[] args)
        {
            PrintMessage(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        private static void ProcessErrors(SqlErrorCollection errors)
        {
            if (errors == null) return;
            foreach (var error in errors.Cast<SqlError>())
            {
                PrintMessage(string.Format(CultureInfo.CurrentCulture, "Sql Error: '{0}' (Severity {1}, State {2})", error.Message, error.Class, error.State));
            }
        }
    }
}