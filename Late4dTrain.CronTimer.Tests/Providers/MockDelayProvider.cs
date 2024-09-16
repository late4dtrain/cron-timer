using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer.Tests.Providers;

public class MockDelayProvider : IDelayProvider
{
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}