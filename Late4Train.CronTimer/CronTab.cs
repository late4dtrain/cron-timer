namespace Late4dTrain.CronTimer
{
    using System;
    using Cronos;

    public class CronTab
    {
        public CronTab(string expression, CronFormat format)
        {
            (Expression, Format, Id) = (expression, format, Guid.NewGuid());
        }

        public Guid Id { get; set; }
        public string Expression { get; set; }
        public CronFormat Format { get; set; }
    }
}