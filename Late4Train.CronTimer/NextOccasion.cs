namespace Late4Train.CronTimer
{
    using System;

    internal class NextOccasion
    {
        public DateTime? NextUtc { get; set; }
        public Guid CronId { get; set; }
        public string CronExpression { get; set; }
    }
}