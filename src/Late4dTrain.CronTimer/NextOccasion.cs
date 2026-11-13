using System;

namespace Late4dTrain.CronTimer
{
    internal class CronNextOccasion
    {
        public DateTime? NextUtc { get; set; }
        public Guid Id { get; set; }
        public string Expression { get; set; }
    }
}
