namespace GSoulavy.CronTimer
{
    using System;
    using System.Threading.Tasks;
    using Cronos;

    public class CronTimer : ICronTimer
    {
        private readonly CronExpression _expression;
        private (TimeSpan elapse, CronResult result) _nextRun;
        private DateTime? _nextUtc;
        public EventHandler<CronEventArgs> TriggeredEventHander;

        public CronTimer(string cronExpression, bool includeSeconds)
        {
            _expression = CronExpression.Parse(cronExpression, includeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard);
        }

        private bool HasNext => _nextRun.result == CronResult.Success;

        public void Start()
        {
            _nextRun = GetNextTimeElapse();
            Run();
        }

        public void Stop()
        {
            _nextRun = (default(TimeSpan), CronResult.Fail);
        }

        private void Run()
        {
            while (HasNext)
            {
                var t = Task.Delay(_nextRun.elapse)
                    .ContinueWith(a => TriggeredEventHander?.Invoke(this, new CronEventArgs()));
                t.Wait();
                _nextRun = GetNextTimeElapse();
            }
        }

        private (TimeSpan elapse, CronResult result) GetNextTimeElapse()
        {
            var now = DateTime.UtcNow;
            _nextUtc = _expression.GetNextOccurrence(now);
            return _nextUtc != null ? 
                (_nextUtc.GetValueOrDefault() - now + TimeSpan.FromMilliseconds(10), CronResult.Success) 
                : (default(TimeSpan), CronResult.Fail);
        }
    }
}