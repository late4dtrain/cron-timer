namespace Late4Train.CronTimer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;

    public class CronTimer : ICronTimer
    {
        private readonly NextTimer _nextTimer;
        private readonly CronOption _option = new CronOption();
        private CancellationTokenSource _cancellationTokenSource;
        private long _interval;
        public EventHandler<CronEventArgs> TriggeredEventHandler;

        public CronTimer(Func<CronOption, CronOption> optionFactory)
        {
            _nextTimer = NextTimer.Create(optionFactory(_option));
        }

        private bool HasNext => _interval > 0;

        public async void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _interval = _nextTimer.GetNextTimeElapse();
            await RunAsync(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _interval = default;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (HasNext && !cancellationToken.IsCancellationRequested)
                try
                {
                    await Task.Delay(_interval.ToMilliseconds(), cancellationToken);
                    TriggeredEventHandler?.Invoke(this,
                        new CronEventArgs(cancellationToken, _nextTimer.CronId, _nextTimer.CronExpression));

                    _interval = _nextTimer.GetNextTimeElapse();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
        }
    }
}