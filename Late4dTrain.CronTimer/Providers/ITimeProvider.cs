using System;

namespace Late4dTrain.CronTimer.Providers
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
