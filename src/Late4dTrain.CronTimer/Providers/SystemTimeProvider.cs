using System;

namespace Late4dTrain.CronTimer.Providers
{
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}