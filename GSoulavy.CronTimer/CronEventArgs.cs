namespace GSoulavy.CronTimer {
    using System;
    using System.Threading;

    public class CronEventArgs : EventArgs
    {
        public DateTime TriggeredUtcDateTime { get; }
        public CancellationToken CancellationToken {get; }
        public CronEventArgs(CancellationToken token)
        {
            CancellationToken = token;
            TriggeredUtcDateTime = DateTime.UtcNow;
        }
    }
}