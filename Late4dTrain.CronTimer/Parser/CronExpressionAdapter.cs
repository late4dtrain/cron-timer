using System;

namespace Late4dTrain.CronTimer.Parser
{
    internal class CronExpressionAdapter
    {
        public Guid Id { get; set; }
        public CronExpression Expression { get; set; }
        public string CronExpression { get; set; }
    }
}
