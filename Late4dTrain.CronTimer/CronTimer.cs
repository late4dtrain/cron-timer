using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Late4dTrain.CronTimer.Abstractions;
using Late4dTrain.CronTimer.Events;
using Late4dTrain.CronTimer.Options;
using Late4dTrain.CronTimer.Parser;
using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer
{
    public sealed class CronTimer : ICronTimer, IDisposable, ICronTimerAsync
    {
        private readonly CronOption _cronOption = new CronOption();
        private readonly ITimeProvider _timeProvider;
        private readonly IDelayProvider _delayProvider;
        private readonly object _stateLock = new object();

        private bool _isRunning;
        private bool _disposed;
        private CancellationTokenSource _cts;
        private Task _task;

        private readonly CronExpressionAdapter[] _expressions;
        private CronNextOccasion _nextOccasion;
        private (bool hasNext, TimeSpan elapse) _nextRun;

        public event EventHandler<CronEventArgs> TriggeredEventHandler;

        public CronTimer(Action<CronOption> action, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null)
        {
            action(_cronOption);
            _expressions = _cronOption.Expressions.Select(e => new CronExpressionAdapter
            {
                Id = e.Id,
                Expression = CronExpression.Parse(e.Expression, e.Formats),
                CronExpression = e.Expression
            }).ToArray();

            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
        }

        public CronTimer(string expression, CronFormats formats, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null)
        {
            var cronExpression = CronExpression.Parse(expression, formats);
            _expressions = new[]
            {
                new CronExpressionAdapter
                {
                    Id = Guid.NewGuid(),
                    Expression = cronExpression,
                    CronExpression = expression
                }
            };

            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
        }

        public void Start(int? executionTimes = null)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                    return; // Timer is already running

                _isRunning = true;
                _cts = new CancellationTokenSource();

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

                        await RunAsync();
                        if (executionTimes != null && --executionTimes <= 0)
                        {
                            _isRunning = false;
                            break;
                        }
                    }
                }, _cts.Token);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken, int? executionTimes = null)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                    return; // Timer is already running

                _isRunning = true;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            await Task.Run(async () =>
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

                    await RunAsync();
                    if (executionTimes != null && --executionTimes <= 0)
                    {
                        _isRunning = false;
                        break;
                    }
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                    return; // Timer is already stopped

                _isRunning = false;
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                    return; // Timer is already stopped

                _isRunning = false;
                _cts.Cancel();
            }

            if (_task != null)
            {
                try
                {
                    await Task.WhenAny(_task,
                        Task.Delay(Timeout.Infinite, cancellationToken)); // Ensure the task completes
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation
                }
            }

            lock (_stateLock)
            {
                _cts.Dispose();
                _cts = null;
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

        private CronNextOccasion GetNextOccasion()
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
                    cronId = expression.Id;
                    cronExpression = expression.CronExpression;
                }
            }

            return new CronNextOccasion
            {
                NextUtc = nextUtc,
                Id = cronId,
                Expression = cronExpression
            };
        }

        private async Task RunAsync()
        {
            try
            {
                var delay = _nextRun.elapse;

                if (delay > TimeSpan.Zero)
                {
                    await _delayProvider.Delay(delay, _cts.Token);
                }

                var triggeredTime = _nextOccasion.NextUtc.GetValueOrDefault();

                TriggeredEventHandler?
                    .Invoke(this, new CronEventArgs(_cts.Token, _nextOccasion.Id,
                        _nextOccasion.Expression, triggeredTime));

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

        private void Dispose(bool disposing)
        {
            lock (_stateLock)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    Stop();

                    if (_task != null)
                    {
                        try
                        {
                            _task.Wait(); // Ensure the task completes
                        }
                        catch (AggregateException ex)
                        {
                            // Handle exceptions from the task, if needed
                        }
                    }

                    _cts?.Dispose();
                    _cts = null;
                    _task = null;
                    TriggeredEventHandler = null;
                }

                _disposed = true;
            }
        }

        ~CronTimer()
        {
            Dispose(false);
        }
    }
}
