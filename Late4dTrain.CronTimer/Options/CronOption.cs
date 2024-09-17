using System.Collections.Generic;

namespace Late4dTrain.CronTimer.Options
{
    public class CronOption
    {
        public List<CronTab> Expressions { get; } = new List<CronTab>();

        public void AddCronTabs(params CronTab[] cronTabs)
        {
            Expressions.AddRange(cronTabs);
        }
    }
}
