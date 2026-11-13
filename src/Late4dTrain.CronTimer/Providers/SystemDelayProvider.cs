using System;
using System.Threading;
using System.Threading.Tasks;

namespace Late4dTrain.CronTimer.Providers
{
    public class SystemDelayProvider : IDelayProvider
    {
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}