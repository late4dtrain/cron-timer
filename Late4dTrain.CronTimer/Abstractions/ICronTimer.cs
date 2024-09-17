namespace Late4dTrain.CronTimer.Abstractions
{
    public interface ICronTimer
    {
        void Start(int? executionTimes = null);
        void Stop();
    }
}
