namespace GSoulavy.CronTimer.Tests
{
    using Xunit;

    public class UnitTest1
    {
        [Fact]
        public void Initialise()
        {
            var timer = new CronTimer("*/1 * * * * *", true);
        }
    }
}