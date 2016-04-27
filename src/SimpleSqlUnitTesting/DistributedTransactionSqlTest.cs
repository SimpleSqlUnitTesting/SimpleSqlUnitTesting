using System.Transactions;

namespace SimpleSqlUnitTesting
{
    public abstract class DistributedTransactionSqlTest : SqlTest
    {
        private TransactionScope _trans;

        protected override void OnBeforeInitializeTest()
        {
            _trans = new TransactionScope(TransactionScopeOption.Required);
        }

        protected override void OnAfterCleanupTest()
        {
            _trans.Dispose();
        }
    }
}