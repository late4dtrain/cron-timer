using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer.Tests.Providers;

public class MockTimeProvider : ITimeProvider
{
    private DateTime _utcNow;

    public MockTimeProvider(DateTime startTime)
    {
        _utcNow = startTime;
    }

    public DateTime UtcNow => _utcNow;

    public void Advance(TimeSpan timeSpan)
    {
        _utcNow = _utcNow.Add(timeSpan);
    }
}
