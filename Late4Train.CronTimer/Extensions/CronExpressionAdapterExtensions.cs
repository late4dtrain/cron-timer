namespace Late4Train.CronTimer.Extensions {
    using System;

    public static class CronExpressionAdapterExtensions
    {
        internal static NextOccasion ToNextOccasion(this CronExpressionAdapter e, DateTime now)
        {
            return new NextOccasion
            {
                CronId = e.CronId,
                NextUtc = e.Expression.GetNextOccurrence(now),
                CronExpression = e.CronExpression
            };
        }
    }
}