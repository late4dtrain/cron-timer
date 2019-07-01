namespace Late4Train.CronTimer.Tests
{
    using Cronos;
    using Xunit;

    public class CronTimerTests
    {
        [Fact]
        public void Initialise()
        {
            var timer = new CronTimer(options =>
            {
                options.AddCronTabs(new CronTab("*/2 * * * * *", CronFormat.IncludeSeconds),
                    new CronTab("*/3 * * * * *", CronFormat.IncludeSeconds));
                return options;
            });

            timer.Start();
            timer.Stop();
        }
    }
}