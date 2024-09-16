using System;
using System.Threading;

namespace Late4dTrain.CronTimer
{
    public class CronEventArgs : EventArgs
    {
        public CronEventArgs(CancellationToken cancellationToken, Guid cronId, string cronExpression,
            DateTime triggeredUtcDateTime)
        {
            (CancellationToken, TriggeredUtcDateTime, CronId, CronExpression, TriggeredUtcDateTime) =
                (cancellationToken, DateTime.UtcNow, cronId, cronExpression, triggeredUtcDateTime);
        }

        public Guid CronId { get; }
        public DateTime TriggeredUtcDateTime { get; }
        public CancellationToken CancellationToken { get; }
        public string CronExpression { get; }
    }
}
