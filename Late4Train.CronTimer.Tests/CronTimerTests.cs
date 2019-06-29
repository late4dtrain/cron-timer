namespace Late4Train.CronTimer.Tests
{
    using Late4Train.CronTimer;
    using Xunit;

    public class CronTimerTests
    {
        [Fact]
        public void Initialise()
        {
            var timer = new CronTimer("*/1 * * * * *", true);
        }
    }
}