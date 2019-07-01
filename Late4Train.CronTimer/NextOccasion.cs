namespace Late4Train.CronTimer
{
    using System;

    internal class NextOccasion
    {
        public long Interval { get; set; }
        public Guid CronId { get; set; }
        public string CronExpression { get; set; }
    }
}