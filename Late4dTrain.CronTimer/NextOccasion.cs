using System;

namespace Late4dTrain.CronTimer
{
    internal class NextOccasion
    {
        public DateTime? NextUtc { get; set; }
        public Guid CronId { get; set; }
        public string CronExpression { get; set; }
    }
}
