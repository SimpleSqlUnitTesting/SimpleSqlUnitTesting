using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.Data.Tools.Schema.Sql.UnitTesting.Conditions;
using System.Globalization;

namespace Frontiers.Impact.ImpactDB.Tests.Framework
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
            string[] expectedStrings,
            bool allowStringNulls = false)
        {
            var jagged = new[] {expectedStrings};
            actions.TestAction.ResultsetShouldBe(resultSet, JaggedToRectangular(jagged), allowStringNulls);
            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBe(this SqlDatabaseTestActions actions,
            int resultSet,
            string[,] expectedStrings,
            bool allowStringNulls = false)
        {
            actions.TestAction.ResultsetShouldBe(resultSet, expectedStrings, allowStringNulls);
            return actions;
        }

        public static SqlDatabaseTestActions ResultsetShouldBe(this SqlDatabaseTestActions actions,
            int resultSet,
            string expectedStrings,
            bool allowStringNulls = false)
        {
			var rows = expectedStrings
				.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.TrimStart('\t'))
				.Select(x => x.Split('\t'))
                .ToArray();

			actions.TestAction.ResultsetShouldBe(resultSet, JaggedToRectangular(rows), allowStringNulls);
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


        public static SqlDatabaseTestActions ResultsetShouldNotBeEmpty(this SqlDatabaseTestActions actions,
            int resultSet)
        {
            actions.TestAction.AddConditions(new NotEmptyResultSetCondition
            {
                ResultSet = resultSet,
                Enabled = true,
                Name = $"that resultset #{resultSet} should not be empty"
            });

            return actions;
        }

        private static SqlDatabaseTestAction ResultsetShouldBe(this SqlDatabaseTestAction action,
            int resultSet,
            string[,] expectedStrings,
            bool allowStringNulls)
        {
            return action.AddConditions(ConditionsForTable(resultSet, expectedStrings, allowStringNulls).ToArray());
        }

        private static SqlDatabaseTestAction AddConditions(this SqlDatabaseTestAction action,
            params TestCondition[] conditions)
        {
            foreach (var testCondition in conditions)
            {
                action.Conditions.Add(testCondition);
            }
            return action;
        }

        private static IEnumerable<TestCondition> ConditionsForTable(
            int resultSet,
            string[,] expectedStrings,
            bool allowStringNulls)
        {
            var rowCount = expectedStrings.GetLength(0);
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                for (int colIndex = 0; colIndex < expectedStrings.GetLength(1); colIndex++)
                {
					double value;
					var expectedValue = double.TryParse(expectedStrings[rowIndex, colIndex], NumberStyles.Any , CultureInfo.InvariantCulture, out value)
						? value.ToString()
						: expectedStrings[rowIndex, colIndex];

                    if (allowStringNulls && expectedValue == "NULL")
                    {
                        expectedValue = null;
                    }

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