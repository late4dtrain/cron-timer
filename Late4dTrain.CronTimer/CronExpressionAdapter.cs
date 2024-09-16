using System;

namespace Late4dTrain.CronTimer
{
    internal class CronExpressionAdapter
    {
        public Guid CronId { get; set; }
        public CronExpression Expression { get; set; }
        public string CronExpression { get; set; }
    }
}
