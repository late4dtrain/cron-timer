namespace Late4dTrain.CronTimer
{
    using System;
    using System.Threading;

    public class CronEventArgs : EventArgs
    {
        public CronEventArgs(CancellationToken cancellationToken, Guid cronId, string cronExpression)
        {
            (CancellationToken, TriggeredUtcDateTime, CronId, CronExpression) =
                (cancellationToken, DateTime.UtcNow, cronId, cronExpression);
        }

        public Guid CronId { get; }
        public DateTime TriggeredUtcDateTime { get; }
        public CancellationToken CancellationToken { get; }
        public string CronExpression { get; }
    }
}