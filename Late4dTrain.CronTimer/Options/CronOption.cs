using Late4dTrain.CronTimer.Parser;

namespace Late4dTrain.CronTimer.Options
{
    public class CronOption
    {
        public CronTab Expression { get; private set; }

        public void AddCronTab(CronTab cronTab)
        {
            Expression = cronTab;
        }
    }
}
