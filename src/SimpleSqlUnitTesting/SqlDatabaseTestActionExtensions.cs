using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting.Conditions;

namespace SimpleSqlUnitTesting
{
    public static class SqlDatabaseTestActionExtensions
    {

        public static SqlDatabaseTestActions SetPretest(this SqlDatabaseTestActions actions, string sql)
        {
            actions.PretestAction = new SqlDatabaseTestAction
            {
                SqlScript = sql
            };

            return actions;
        }

        public static SqlDatabaseTestActions SetPosttest(this SqlDatabaseTestActions actions, string sql)
        {
            actions.PosttestAction = new SqlDatabaseTestAction
            {
                SqlScript = sql
            };

            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBe(this SqlDatabaseTestActions actions,
            int resultSet,
            string[] expectedStrings)
        {
            var jagged = new[] {expectedStrings};
            actions.TestAction.ResultsetShouldBe(resultSet, JaggedToRectangular(jagged));
            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBe(this SqlDatabaseTestActions actions,
            int resultSet,
            string[,] expectedStrings)
        {
            actions.TestAction.ResultsetShouldBe(resultSet, expectedStrings);
            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBe(this SqlDatabaseTestActions actions,
            int resultSet,
            string expectedStrings)
        {
            var rows = expectedStrings
                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('\t'))
                .ToArray();
            actions.TestAction.ResultsetShouldBe(resultSet, JaggedToRectangular(rows));
            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBeEmpty(this SqlDatabaseTestActions actions,
            int resultSet)
        {
            actions.TestAction.AddConditions(new EmptyResultSetCondition
            {
                ResultSet = resultSet,
                Enabled = true,
                Name = $"that resultset #{resultSet} should be empty"
            });

            return actions;
        }

        private static void ResultsetShouldBe(this SqlDatabaseTestAction action,
            int resultSet,
            string[,] expectedStrings)
        {
            action.AddConditions(ConditionsForTable(resultSet, expectedStrings).ToArray());
        }

        private static void AddConditions(this SqlDatabaseTestAction action,
            params TestCondition[] conditions)
        {
            foreach (var testCondition in conditions)
            {
                action.Conditions.Add(testCondition);
            }
        }

        private static IEnumerable<TestCondition> ConditionsForTable(
            int resultSet,
            string[,] expectedStrings)
        {
            var rowCount = expectedStrings.GetLength(0);
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                for (int colIndex = 0; colIndex < expectedStrings.GetLength(1); colIndex++)
                {

                    var expectedValue = expectedStrings[rowIndex, colIndex];
                    yield return new ScalarValueCondition
                    {
                        ColumnNumber = colIndex + 1,
                        Enabled = true,
                        ExpectedValue = expectedValue,
                        Name = $"that value at resultset #{resultSet}, row #{rowIndex + 1}, column #{colIndex + 1} should be {expectedValue}",
                        NullExpected = expectedValue == null,
                        ResultSet = resultSet,
                        RowNumber = rowIndex + 1
                    };
                }

            yield return new RowCountCondition
            {
                Enabled = true,
                Name = $"that resultset #{resultSet} has {rowCount} rows",
                ResultSet = resultSet,
                RowCount = expectedStrings.GetLength(0)
            };
        }

        private static T[,] JaggedToRectangular<T>(T[][] source)
        {
            try
            {
                int rowCount = source.Length;
                int columnCount = source.GroupBy(row => row.Length).Single().Key;

                var result = new T[rowCount, columnCount];
                for (int i = 0; i < rowCount; ++i)
                    for (int j = 0; j < columnCount; ++j)
                        result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            }
        }
    }
}