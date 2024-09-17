using System;
using System.Threading;

namespace Late4dTrain.CronTimer
{
    public class CronEventArgs : EventArgs
    {
        public CronEventArgs(CancellationToken cancellationToken, Guid id, string expression,
            DateTime triggeredUtcDateTime)
        {
            CancellationToken = cancellationToken;
            Id = id;
            Expression = expression;
            TriggeredUtcDateTime = triggeredUtcDateTime;
        }

        public Guid Id { get; }
        public DateTime TriggeredUtcDateTime { get; }
        public CancellationToken CancellationToken { get; }
        public string Expression { get; }
    }
}
