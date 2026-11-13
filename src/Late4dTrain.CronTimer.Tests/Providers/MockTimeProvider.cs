using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer.Tests.Providers;

public class MockTimeProvider : ITimeProvider
{
    private readonly object _lock = new();
    private DateTime _utcNow;

    public MockTimeProvider(DateTime startTime)
    {
        _utcNow = startTime;
    }

    public DateTime UtcNow
    {
        get { lock (_lock) return _utcNow; }
    }

    public void Advance(TimeSpan timeSpan)
    {
        lock (_lock) _utcNow = _utcNow.Add(timeSpan);
    }
}
