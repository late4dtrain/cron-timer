using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer
{
    public class CronTimer : ICronTimer, IDisposable
    {
        private readonly CronOption _cronOption = new CronOption();
        private readonly CronExpressionAdapter[] _expressions;
        private CancellationTokenSource _cancellationTokenSource;
        private NextOccasion _nextOccasion;
        private (TimeSpan elapse, CronResult result) _nextRun;
        public EventHandler<CronEventArgs> TriggeredEventHandler;

        private readonly ITimeProvider _timeProvider;
        private readonly IDelayProvider _delayProvider;

        private int? _executionTimes;
        private bool HasNextExecution => _executionTimes.HasValue && _executionTimes.Value > 0;

        public (TimeSpan elapse, CronResult result) NextRun => _nextRun;

        public CronTimer(Action<CronOption> action, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null)
        {
            action(_cronOption);
            _expressions = _cronOption.Expressions.Select(e => new CronExpressionAdapter
            {
                CronId = e.Id,
                Expression = CronExpression.Parse(e.Expression, e.ExpressionType),
                CronExpression = e.Expression
            }).ToArray();

            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
        }

        private bool HasNext => _nextRun.result == CronResult.Success;

        public void Start(CancellationToken cancellationToken, int? executionTimes = null)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executionTimes = executionTimes;

            _nextRun = GetNextTimeElapse();
            Task.Run(() => RunAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _nextRun = (default, CronResult.Fail);
            Dispose();
        }

        private (TimeSpan elapse, CronResult result) GetNextTimeElapse()
        {
            var dtNow = _timeProvider.UtcNow;
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

        private async Task RunOnceAsync()
        {
            if (HasNext && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await _delayProvider.Delay(_nextRun.elapse, _cancellationTokenSource.Token);

                    var triggeredTime = _nextOccasion.NextUtc.GetValueOrDefault();

                    TriggeredEventHandler?.Invoke(this,
                        new CronEventArgs(_cancellationTokenSource.Token, _nextOccasion.CronId,
                            _nextOccasion.CronExpression, triggeredTime));

                    _nextRun = GetNextTimeElapse();
                }
                catch (TaskCanceledException)
                {
                    // Handle cancellation
                }
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (HasNext
                   && !cancellationToken.IsCancellationRequested
                   && HasNextExecution)
            {
                await RunOnceAsync();
                if (_executionTimes.HasValue)
                {
                    _executionTimes--;
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            TriggeredEventHandler = null;
        }
    }
}
