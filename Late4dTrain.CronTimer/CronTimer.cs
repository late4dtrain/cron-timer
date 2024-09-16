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
        private readonly ITimeProvider _timeProvider;
        private readonly IDelayProvider _delayProvider;
        private readonly object _stateLock = new object();

        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;

        private CronExpressionAdapter[] _expressions;
        private NextOccasion _nextOccasion;
        private (bool hasNext, TimeSpan elapse) _nextRun;

        public event EventHandler<CronEventArgs> TriggeredEventHandler;

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

        public void Start(CancellationToken cancellationToken, int? executionTimes = null)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                    return; // Timer is already running

                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _task = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (!_isRunning)
                            break;

                        _nextRun = GetNextTimeElapse();

                        if (!_nextRun.hasNext)
                        {
                            _isRunning = false;
                            break;
                        }

                        await RunOnceAsync();
                        if (executionTimes != null && --executionTimes <= 0)
                        {
                            _isRunning = false;
                            break;
                        }
                    }
                }, _cancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                    return; // Timer is already stopped

                _isRunning = false;
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private (bool hasNext, TimeSpan elapse) GetNextTimeElapse()
        {
            lock (_stateLock)
            {
                _nextOccasion = GetNextOccasion();

                if (_nextOccasion.NextUtc == null)
                    return (false, TimeSpan.Zero);

                var delay = _nextOccasion.NextUtc.Value - _timeProvider.UtcNow;
                return (true, delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
            }
        }

        private NextOccasion GetNextOccasion()
        {
            DateTime? nextUtc = null;
            Guid cronId = Guid.Empty;
            string cronExpression = null;

            foreach (var expression in _expressions)
            {
                var occurrence = expression.Expression.GetNextOccurrence(_timeProvider.UtcNow);
                if (occurrence.HasValue && (nextUtc == null || occurrence < nextUtc))
                {
                    nextUtc = occurrence;
                    cronId = expression.CronId;
                    cronExpression = expression.CronExpression;
                }
            }

            return new NextOccasion
            {
                NextUtc = nextUtc,
                CronId = cronId,
                CronExpression = cronExpression
            };
        }

        private async Task RunOnceAsync()
        {
            try
            {
                var delay = _nextRun.elapse;

                if (delay > TimeSpan.Zero)
                {
                    await _delayProvider.Delay(delay, _cancellationTokenSource.Token);
                }

                var triggeredTime = _nextOccasion.NextUtc.GetValueOrDefault();

                TriggeredEventHandler?
                    .Invoke(this, new CronEventArgs(_cancellationTokenSource.Token, _nextOccasion.CronId,
                        _nextOccasion.CronExpression, triggeredTime));

                _nextRun = GetNextTimeElapse();
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (ObjectDisposedException)
            {
                // Handle disposal
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                _cancellationTokenSource?.Dispose();
                _task = null;
                TriggeredEventHandler = null;
            }
        }
    }
}
