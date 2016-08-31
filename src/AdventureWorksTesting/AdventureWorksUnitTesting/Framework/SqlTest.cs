using System;
using System.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;

namespace Frontiers.Impact.ImpactDB.Tests.Framework
{
    public abstract class SqlTest
    {
        protected SqlDatabaseTestAction TestCleanupAction { get; set; }
        protected SqlDatabaseTestAction TestInitializeAction { get; set; }

        private void CleanupTest(SqlDatabaseTestActions testActions)
        {
            if (TestCleanupAction != null)
            {
                Trace.WriteLine("Executing test cleanup script...");
                TestService.Execute(ExecutionContext, ExecutionContext, TestCleanupAction);
            }

            if (ExecutionContext != null)
            {
                ExecutionContext.Dispose();
                ExecutionContext = null;
            }

            var allSql = new[] {
                "----------------SQL EXECUTED---------------------",
                "-- TEST INIT",
                TestInitializeAction?.SqlScript,
                "-- PRETEST",
                testActions?.PretestAction?.SqlScript,
                "-- TEST",
                testActions?.TestAction?.SqlScript,
                "-- POSTTEST",
                testActions?.PosttestAction?.SqlScript,
                "-- TEST CLEANUP",
                TestCleanupAction?.SqlScript,
                "-------------------------------------------------",
            };

            foreach (var line in allSql)
            {
                Trace.WriteLine(line);
            }

            OnAfterCleanupTest();
        }
        protected virtual void OnAfterCleanupTest() { }

        private void InitializeTest()
        {
            OnBeforeInitializeTest();
            ExecutionContext = TestService.OpenExecutionContext();
            if (TestInitializeAction != null)
            {
                Trace.WriteLine("Executing test initialization script...");
                TestService.Execute(ExecutionContext, ExecutionContext, TestInitializeAction);
            }
        }

        protected virtual void OnBeforeInitializeTest() { }

        protected ConnectionContext ExecutionContext { get; set; }

        public static SqlDatabaseTestService2 TestService { get; } = new SqlDatabaseTestService2();

        protected void RunTest(
            SqlDatabaseTestActions testActions,
            params ResultsetSorting[] sortings)
        {
            InitializeTest();
            Trace.WriteLineIf(testActions.PretestAction != null, "Executing pre-test script...");
            ExecutionContext.CommandTimeout = 60;
            TestService.Execute(ExecutionContext, ExecutionContext, testActions.PretestAction);

            try
            {
                Trace.WriteLineIf(testActions.TestAction != null, "Executing test script...");
                TestService.ExecuteWithSortings(ExecutionContext, ExecutionContext, testActions.TestAction, sortings);
            }
            finally
            {
                try
                {
                    Trace.WriteLineIf(testActions.PosttestAction != null, "Executing post-test script...");
                    TestService.Execute(ExecutionContext, ExecutionContext, testActions.PosttestAction);
                }
                finally
                {
                    CleanupTest(testActions);
                }
            }
        }
    }
}