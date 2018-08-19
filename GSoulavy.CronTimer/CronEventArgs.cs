namespace GSoulavy.CronTimer {
    using System;

    public class CronEventArgs : EventArgs
    {
        public DateTime TriggeredUtcDateTime { get;}
        public CronEventArgs()
        {
            TriggeredUtcDateTime = DateTime.UtcNow;
        }
    }
}