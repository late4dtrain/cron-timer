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
    public sealed class CronTimer : ICronTimer, ICronTimerAsync, IDisposable
    {
        private readonly CronOption _cronOption = new();
        private readonly ITimeProvider _timeProvider;
        private readonly IDelayProvider _delayProvider;
        private readonly object _stateLock = new();
        private readonly int _executionTimeout;

        private bool _isRunning;
        private bool _disposed;
        private CancellationTokenSource _cts;
        private Task _task;

        private readonly CronExpressionAdapter[] _expressions;
        private CronNextOccasion _nextOccasion;
        private (bool hasNext, TimeSpan elapse) _nextRun;
        private readonly Action<string, Exception> _errorLogger;
        private readonly Action<string> _infoLogger;

        public event EventHandler<CronEventArgs> TriggeredEventHandler;

        public CronTimer(Action<CronOption> action, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null, int executionTimeout = Timeout.Infinite,
            Action<string, Exception> errorLogger = null,
            Action<string> infoLogger = null)
        {
            action(_cronOption);
            var cronTab = _cronOption.Expression;
            _expressions = new[]
            {
                new CronExpressionAdapter
                {
                    Id = cronTab.Id,
                    Expression = CronExpression.Parse(cronTab.Expression, cronTab.Formats),
                    CronExpression = cronTab.Expression
                }
            };

            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
            _executionTimeout = executionTimeout;
            _errorLogger = errorLogger;
            _infoLogger = infoLogger;
        }

        public CronTimer(string expression, CronFormats formats, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null, int executionTimeout = Timeout.Infinite)
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
            _executionTimeout = executionTimeout;
        }

        public void Start()
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
                    }
                }, _cts.Token);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
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
            var cronId = Guid.Empty;
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
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var delay = _nextRun.elapse;

                    if (delay > TimeSpan.Zero)
                    {
                        await _delayProvider.Delay(delay, _cts.Token);
                    }

                    var triggeredTime = _nextOccasion.NextUtc.GetValueOrDefault();

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                    timeoutCts.CancelAfter(_executionTimeout);

                    try
                    {
                        await Task.Run(() =>
                        {
                            TriggeredEventHandler?.Invoke(this, new CronEventArgs(timeoutCts.Token, _nextOccasion.Id,
                                _nextOccasion.Expression, triggeredTime));
                        }, timeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        LogInfo($"Operation cancelled due to timeout for expression: {_nextOccasion.Expression}");
                    }

                    // Only update _nextRun if the operation wasn't cancelled
                    if (!timeoutCts.IsCancellationRequested)
                    {
                        _nextRun = GetNextTimeElapse();
                    }
                    else
                    {
                        // If cancelled, wait for the next scheduled time
                        await _delayProvider.Delay(TimeSpan.FromSeconds(1), _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    LogInfo("CronTimer operation cancelled.");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    LogError("CronTimer object disposed unexpectedly.", new ObjectDisposedException(nameof(CronTimer)));
                    break;
                }
                catch (Exception ex)
                {
                    LogError("Unexpected error in CronTimer.", ex);
                }
            }
        }

        private void LogError(string message, Exception ex)
        {
            _errorLogger?.Invoke(message, ex);
        }

        private void LogInfo(string message)
        {
            _infoLogger?.Invoke(message);
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
                            LogError("Error occurred while waiting for the task to complete.", ex);
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
