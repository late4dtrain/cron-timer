namespace Late4Train.CronTimer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cronos;

    public class CronTimer : ICronTimer
    {
        private readonly CronExpression _expression;
        private CancellationTokenSource _cancellationTokenSource;
        private (TimeSpan elapse, CronResult result) _nextRun;
        private DateTime? _nextUtc;
        public EventHandler<CronEventArgs> TriggeredEventHander;

        public CronTimer(string cronExpression, bool includeSeconds = false)
        {
            _expression = CronExpression.Parse(cronExpression,
                includeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard);
        }

        private bool HasNext => _nextRun.result == CronResult.Success;

        public async void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _nextRun = GetNextTimeElapse();
            await RunAsync(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _nextRun = (default, CronResult.Fail);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (HasNext)
                try
                {
                    await Task.Delay(_nextRun.elapse, cancellationToken);
                    TriggeredEventHander?.Invoke(this, new CronEventArgs(cancellationToken));

                    _nextRun = GetNextTimeElapse();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
        }

        private (TimeSpan elapse, CronResult result) GetNextTimeElapse()
        {
            var now = DateTime.UtcNow;
            _nextUtc = _expression.GetNextOccurrence(now);
            return _nextUtc != null
                ? (_nextUtc.GetValueOrDefault() - now + TimeSpan.FromMilliseconds(10), CronResult.Success)
                : (default, CronResult.Fail);
        }
    }
}