using Microsoft.Data.Tools.Schema.Sql.UnitTesting;

namespace SimpleSqlUnitTesting
{
    public abstract class LocalTransactionSqlTest : SqlTest
    {
        protected override void OnBeforeInitializeTest()
        {
            if (TestInitializeAction == null)
            {
                TestInitializeAction = new SqlDatabaseTestAction();
            }

            TestInitializeAction.SqlScript =
                $"BEGIN TRAN\nSAVE TRAN TestRun\n{TestInitializeAction.SqlScript}";
            if (TestCleanupAction == null)
            {
                TestCleanupAction = new SqlDatabaseTestAction();
            }

            TestCleanupAction.SqlScript =
                $"{TestCleanupAction.SqlScript}\nROLLBACK TRAN TestRun";
        }
    }
}