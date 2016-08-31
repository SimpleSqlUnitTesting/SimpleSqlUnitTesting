using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frontiers.Impact.ImpactDB.Tests.Framework
{
    public class SqlDatabaseTestService2 : SqlDatabaseTestService
    {
        public void ExecuteWithSortings(
            ConnectionContext scriptExecutionContext, 
            ConnectionContext privilegedExecutionContext, 
            SqlDatabaseTestAction action,
            ResultsetSorting[] sortings, 
            params DbParameter[] sqlParameters)
        {
            if (action == null)
            {
                return;
            }
            if (scriptExecutionContext == null)
            {
                throw new AssertFailedException("Script execution context cannot be null");
            }
            if (privilegedExecutionContext == null)
            {
                throw new AssertFailedException("Privileged execution context cannot be null");
            }
            string sqlScript = action.SqlScript;
            if (string.IsNullOrEmpty(sqlScript?.Trim()) && !action.Conditions.Any())
            {
                Trace.WriteLine("Skipping execution...");
                return;
            }
            SqlExecutionResult[] results = ExecutionEngine2.ExecuteTest(scriptExecutionContext, action.SqlScript, sqlParameters);
            var tables = results[0].DataSet.Tables.Cast<DataTable>().ToArray();
            results[0].DataSet.Tables.Clear();
            int i = 1;
            foreach (var table in tables)
            {
                var sorting = sortings.FirstOrDefault(x => x.ResultsetNumber == i);

                if (sorting == null)
                {
                    results[0].DataSet.Tables.Add(table);
                }
                else
                {
                    table.DefaultView.Sort = sorting.ResultsetSortExpression;
                    results[0].DataSet.Tables.Add(table.DefaultView.ToTable());
                }
                i += 1;
            }
        
            ExecutionEngine2.EvaluateConditions(privilegedExecutionContext.Connection, results, action.Conditions);
        }

    }
}