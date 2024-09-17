using System.Threading;
using System.Threading.Tasks;

namespace Late4dTrain.CronTimer.Abstractions
{
    public interface ICronTimerAsync
    {
        Task StartAsync(CancellationToken cancellationToken, int? executionTimes = null);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
