namespace Late4dTrain.CronTimer.Tests
{
    using Cronos;
    using Late4dTrain.CronTimer;
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
            });

            timer.Start();
            timer.Stop();
        }
    }
}