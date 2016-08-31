using System;
using Frontiers.Impact.ImpactDB.Tests.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdventureWorksUnitTesting
{
    [TestClass]
    public class uspUpdateEmployeeHireInfo : LocalTransactionSqlTest
    {

        public uspUpdateEmployeeHireInfo()
        {
            TestInitializeAction = Actions.CreateSingle(@"
INSERT INTO [Production].[Location]
VALUES ('TEST',100,100,'20160101');
");

        }

        [TestMethod]
        public void ReturnsData()
        {
            RunTest(Actions.CreateBlock(@"
				SELECT  [Name]
                       ,[CostRate]
                       ,[Availability]
                       ,[ModifiedDate] 
                FROM [Production].[Location]")
                .ResultsetShouldBe(1, @"
				TEST	100.00	100.00	2016-01-01 00:00:00.000"));
        }

        [TestMethod]
        public void NonReturnsData()
        {
            RunTest(Actions.CreateBlock(@"
DELETE [Production].[Location]
WHERE Name='TEST' 

SELECT [Name]
                       ,[CostRate]
                       ,[Availability]
                       ,[ModifiedDate] 
                FROM [Production].[Location]").ResultsetShouldBeEmpty(1));

        }

    }
}
