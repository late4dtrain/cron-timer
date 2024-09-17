using System.Threading;
using System.Threading.Tasks;

namespace Late4dTrain.CronTimer
{
    public interface ICronTimer
    {
        void Start(int? executionTimes = null);
        void Stop();
    }
}
