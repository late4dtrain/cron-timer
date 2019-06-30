namespace Late4Train.CronTimer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Cronos;

    public class CronTimer : ICronTimer
    {
        private readonly CronOption _cronOption = new CronOption();
        private readonly CronExpressionAdapter[] _expressions;
        private CancellationTokenSource _cancellationTokenSource;
        private NextOccasion _nextOccasion;
        private (TimeSpan elapse, CronResult result) _nextRun;
        public EventHandler<CronEventArgs> TriggeredEventHandler;

        public CronTimer(Action<CronOption> action)
        {
            action(_cronOption);
            _expressions = _cronOption.Expressions.Select(e => new CronExpressionAdapter
            {
                CronId = e.Id,
                Expression = CronExpression.Parse(e.Expression, e.Format),
                CronExpression = e.Expression
            }).ToArray();
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
            while (HasNext && !cancellationToken.IsCancellationRequested)
                try
                {
                    await Task.Delay(_nextRun.elapse, cancellationToken);
                    TriggeredEventHandler?.Invoke(this,
                        new CronEventArgs(cancellationToken, _nextOccasion.CronId, _nextOccasion.CronExpression));

                    _nextRun = GetNextTimeElapse();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
        }

        private (TimeSpan elapse, CronResult result) GetNextTimeElapse()
        {
            var dtNow = DateTime.UtcNow;
            var now = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second,
                DateTimeKind.Utc);
            _nextOccasion = _expressions.OrderBy(e => e.Expression.GetNextOccurrence(now)).Select(e =>
                new NextOccasion
                {
                    CronId = e.CronId,
                    NextUtc = e.Expression.GetNextOccurrence(now),
                    CronExpression = e.CronExpression
                }).FirstOrDefault();
            return _nextOccasion?.NextUtc != null
                ? (_nextOccasion.NextUtc.GetValueOrDefault() - now, CronResult.Success)
                : (default, CronResult.Fail);
        }
    }
}