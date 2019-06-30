namespace Late4Train.CronTimer
{
    using System;
    using Cronos;

    internal class CronExpressionAdapter
    {
        public Guid CronId { get; set; }
        public CronExpression Expression { get; set; }
        public string CronExpression { get; set; }
    }
}