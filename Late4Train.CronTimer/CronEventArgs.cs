namespace Late4Train.CronTimer
{
    using System;
    using System.Threading;

    public class CronEventArgs : EventArgs
    {
        public CronEventArgs(CancellationToken token)
        {
            CancellationToken = token;
            TriggeredUtcDateTime = DateTime.UtcNow;
        }

        public DateTime TriggeredUtcDateTime { get; }
        public CancellationToken CancellationToken { get; }
    }
}