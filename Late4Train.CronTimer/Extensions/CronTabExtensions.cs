namespace Late4Train.CronTimer.Extensions
{
    using Cronos;

    public static class CronTabExtensions
    {
        internal static CronExpressionAdapter ToExpressionAdapter(this CronTab cronTab)
        {
            return new CronExpressionAdapter
            {
                CronId = cronTab.Id,
                Expression = CronExpression.Parse(cronTab.Expression, cronTab.Format),
                CronExpression = cronTab.Expression
            };
        }
    }
}