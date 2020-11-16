namespace Late4dTrain.CronTimer
{
    using System.Collections.Generic;

    public class CronOption
    {
        public List<CronTab> Expressions = new List<CronTab>();

        public void AddCronTabs(params CronTab[] cronTabs)
        {
            Expressions.AddRange(cronTabs);
        }
    }
}