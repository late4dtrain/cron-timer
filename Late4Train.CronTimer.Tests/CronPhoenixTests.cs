namespace Late4Train.CronTimer.Tests
{
    using System;
    using System.Collections.Generic;
    using Cronos;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using Xunit;

    public class CronPhoenixTests
    {
        public static IEnumerable<object[]> GetCronFacts()
        {
            yield return new object[]
            {
                "* * * * * *", CronFormat.IncludeSeconds, new DateTime(2019, 06, 29, 12, 00, 01, DateTimeKind.Utc),
                new DateTime(2019, 06, 29, 12, 00, 02, DateTimeKind.Utc)
            };
        }

        [Theory]
        [MemberData(nameof(GetCronFacts))]
        public void Original(string expression, CronFormat format, DateTime testDate, DateTime? nextDate)
        {
            var cronExpression = CronExpression.Parse(expression, format);

            var next = cronExpression.GetNextOccurrence(testDate);

            using (new AssertionScope())
            {
                next.Should().NotBeNull();
                next.Should().Be(nextDate);
            }
        }
    }
}