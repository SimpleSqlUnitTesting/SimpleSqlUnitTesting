namespace Frontiers.Impact.ImpactDB.Tests.Framework
{
    public class ResultsetSorting
    {
        public ResultsetSorting(int resultsetNumber, string resultsetSortExpression)
        {
            ResultsetNumber = resultsetNumber;
            ResultsetSortExpression = resultsetSortExpression;
        }
        public int ResultsetNumber { get; }
        public string ResultsetSortExpression { get; }
    }
}