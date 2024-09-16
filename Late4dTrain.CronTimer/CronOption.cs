using System.Collections.Generic;

namespace Late4dTrain.CronTimer
{
    public class CronOption
    {
        public List<CronTab> Expressions = new List<CronTab>();

        public void AddCronTabs(params CronTab[] cronTabs)
        {
            Expressions.AddRange(cronTabs);
        }
    }
}
