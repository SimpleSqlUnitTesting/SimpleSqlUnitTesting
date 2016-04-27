using System;
using System.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;

namespace SimpleSqlUnitTesting
{
    public abstract class SqlTest
    {
        private static SqlDatabaseTestService _testService = new SqlDatabaseTestService();
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

        public static SqlDatabaseTestService TestService
        {
            get { return _testService; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _testService = value;
            }
        }

        protected void RunTest(SqlDatabaseTestActions testActions)
        {
            InitializeTest();
            Trace.WriteLineIf(testActions.PretestAction != null, "Executing pre-test script...");
            TestService.Execute(ExecutionContext, ExecutionContext, testActions.PretestAction);

            try
            {
                Trace.WriteLineIf(testActions.TestAction != null, "Executing test script...");
                TestService.Execute(ExecutionContext, ExecutionContext, testActions.TestAction);
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