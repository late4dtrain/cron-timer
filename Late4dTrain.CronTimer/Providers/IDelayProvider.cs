using System;
using System.Threading;
using System.Threading.Tasks;

namespace Late4dTrain.CronTimer.Providers
{
    public interface IDelayProvider
    {
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }
}