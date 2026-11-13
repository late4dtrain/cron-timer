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
        private sealed class RunState
        {
            public RunState(CancellationTokenSource cancellation)
            {
                Cancellation = cancellation;
            }

            public CancellationTokenSource Cancellation { get; }
            public Task Worker { get; set; }
        }

        private static readonly AsyncLocal<CronTimer> ActiveCallback = new AsyncLocal<CronTimer>();

        private readonly ITimeProvider _timeProvider;
        private readonly IDelayProvider _delayProvider;
        private readonly object _stateLock = new object();
        private readonly int _executionTimeout;
        private readonly CronExpressionAdapter[] _expressions;
        private readonly Action<string, Exception> _errorLogger;
        private readonly Action<string> _infoLogger;

        private RunState _run;
        private bool _disposed;

        public event EventHandler<CronEventArgs> TriggeredEventHandler;
        public event Func<object, CronEventArgs, Task> TriggeredAsyncEventHandler;

        public CronTimer(Action<CronOption> action, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null, int executionTimeout = Timeout.Infinite,
            Action<string, Exception> errorLogger = null,
            Action<string> infoLogger = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            ValidateTimeout(executionTimeout);

            var options = new CronOption();
            action(options);
            if (options.Expressions.Any(tab => tab == null))
                throw new ArgumentException("Cron configuration cannot contain null schedules.", nameof(action));

            _expressions = options.Expressions.Select(tab => new CronExpressionAdapter
            {
                Id = tab.Id,
                Expression = CronExpression.Parse(tab.Expression, tab.Formats),
                CronExpression = tab.Expression
            }).ToArray();
            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
            _executionTimeout = executionTimeout;
            _errorLogger = errorLogger;
            _infoLogger = infoLogger;
        }

        public CronTimer(string expression, CronFormats formats, ITimeProvider timeProvider = null,
            IDelayProvider delayProvider = null, int executionTimeout = Timeout.Infinite)
        {
            ValidateTimeout(executionTimeout);
            var parsed = CronExpression.Parse(expression, formats);
            var tab = new CronTab(expression, formats);
            _expressions = new[]
            {
                new CronExpressionAdapter { Id = tab.Id, Expression = parsed, CronExpression = expression }
            };
            _timeProvider = timeProvider ?? new SystemTimeProvider();
            _delayProvider = delayProvider ?? new SystemDelayProvider();
            _executionTimeout = executionTimeout;
        }

        private static void ValidateTimeout(int executionTimeout)
        {
            if (executionTimeout < Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(executionTimeout), "Timeout must be non-negative or Timeout.Infinite.");
        }

        public void Start() => StartAsync(CancellationToken.None);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                ThrowIfDisposed();
                if (_run != null)
                {
                    if (!_run.Worker.IsCompleted) return _run.Worker;
                    _run = null;
                }

                var run = new RunState(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
                _run = run;
                run.Worker = Task.Run(() => RunWorkerAsync(run));
                return run.Worker;
            }
        }

        public void Stop()
        {
            RunState run;
            lock (_stateLock) run = _run;
            Cancel(run);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            RunState run;
            lock (_stateLock) run = _run;
            if (run == null) return;

            Cancel(run);
            if (ReferenceEquals(ActiveCallback.Value, this))
                throw new InvalidOperationException("StopAsync cannot be awaited from a callback owned by this timer.");

            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            var completed = await Task.WhenAny(run.Worker, cancellationTask).ConfigureAwait(false);
            if (!ReferenceEquals(completed, run.Worker))
                throw new OperationCanceledException(cancellationToken);
            await run.Worker.ConfigureAwait(false);
        }

        private async Task RunWorkerAsync(RunState run)
        {
            DateTime? cursor = null;
            try
            {
                while (!run.Cancellation.IsCancellationRequested)
                {
                    var now = _timeProvider.UtcNow;
                    if (cursor.HasValue && cursor.Value > now) now = cursor.Value;
                    var next = FindNext(now);
                    if (next == null) break;

                    var delay = next.Value.Time - now;
                    if (delay > TimeSpan.Zero)
                        await _delayProvider.Delay(delay, run.Cancellation.Token).ConfigureAwait(false);
                    run.Cancellation.Token.ThrowIfCancellationRequested();

                    foreach (var schedule in next.Value.Schedules)
                    {
                        run.Cancellation.Token.ThrowIfCancellationRequested();
                        await TriggerAsync(run, schedule, next.Value.Time).ConfigureAwait(false);
                    }

                    var after = _timeProvider.UtcNow;
                    cursor = after > next.Value.Time ? after : next.Value.Time;
                }
            }
            catch (OperationCanceledException) when (run.Cancellation.IsCancellationRequested)
            {
                LogInfo("CronTimer operation cancelled.");
            }
            catch (Exception ex)
            {
                LogError("CronTimer worker stopped after an unexpected error.", ex);
            }
            finally
            {
                run.Cancellation.Dispose();
            }
        }

        private (DateTime Time, CronExpressionAdapter[] Schedules)? FindNext(DateTime after)
        {
            DateTime? earliest = null;
            var schedules = new System.Collections.Generic.List<CronExpressionAdapter>();
            foreach (var schedule in _expressions)
            {
                var occurrence = schedule.Expression.GetNextOccurrence(after);
                if (!occurrence.HasValue) continue;
                if (!earliest.HasValue || occurrence.Value < earliest.Value)
                {
                    earliest = occurrence.Value;
                    schedules.Clear();
                    schedules.Add(schedule);
                }
                else if (occurrence.Value == earliest.Value)
                {
                    schedules.Add(schedule);
                }
            }
            if (!earliest.HasValue) return null;
            return (earliest.Value, schedules.ToArray());
        }

        private async Task TriggerAsync(RunState run, CronExpressionAdapter schedule, DateTime scheduledTime)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(run.Cancellation.Token))
            {
                if (_executionTimeout != Timeout.Infinite)
                    timeout.CancelAfter(_executionTimeout);

                var args = new CronEventArgs(timeout.Token, schedule.Id, schedule.CronExpression, scheduledTime);
                var syncHandlers = TriggeredEventHandler;
                if (syncHandlers != null)
                {
                    foreach (EventHandler<CronEventArgs> handler in syncHandlers.GetInvocationList())
                    {
                        if (timeout.IsCancellationRequested) break;
                        var previous = ActiveCallback.Value;
                        ActiveCallback.Value = this;
                        try { handler(this, args); }
                        catch (Exception ex) { LogError("CronTimer synchronous callback failed.", ex); }
                        finally { ActiveCallback.Value = previous; }
                    }
                }

                var asyncHandlers = TriggeredAsyncEventHandler;
                if (asyncHandlers != null)
                {
                    foreach (Func<object, CronEventArgs, Task> handler in asyncHandlers.GetInvocationList())
                    {
                        if (timeout.IsCancellationRequested) break;
                        var previous = ActiveCallback.Value;
                        ActiveCallback.Value = this;
                        try
                        {
                            var callback = handler(this, args);
                            if (callback != null) await callback.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
                        catch (Exception ex) { LogError("CronTimer asynchronous callback failed.", ex); }
                        finally { ActiveCallback.Value = previous; }
                    }
                }

                if (timeout.IsCancellationRequested && !run.Cancellation.IsCancellationRequested)
                    LogInfo($"Execution timed out for expression: {schedule.CronExpression}");
            }
        }

        private static void Cancel(RunState run)
        {
            if (run == null) return;
            try { run.Cancellation.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CronTimer));
        }

        private void LogError(string message, Exception exception)
        {
            try { _errorLogger?.Invoke(message, exception); }
            catch { /* Logging must not destabilize the timer worker. */ }
        }

        private void LogInfo(string message)
        {
            try { _infoLogger?.Invoke(message); }
            catch { /* Logging must not destabilize the timer worker. */ }
        }

        public void Dispose()
        {
            RunState run;
            lock (_stateLock)
            {
                if (_disposed) return;
                _disposed = true;
                run = _run;
                TriggeredEventHandler = null;
                TriggeredAsyncEventHandler = null;
            }
            Cancel(run);
            GC.SuppressFinalize(this);
        }
    }
}
