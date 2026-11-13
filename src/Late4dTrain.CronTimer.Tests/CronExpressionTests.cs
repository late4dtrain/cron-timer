using FluentAssertions;
using Late4dTrain.CronTimer.Parser;

namespace Late4dTrain.CronTimer.Tests
{
    public class CronExpressionTests
    {
        [Theory]
        [InlineData("*/5 * * * * *", CronFormats.IncludeSeconds,
            new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 })]
        [InlineData("0-30/10 * * * * *", CronFormats.IncludeSeconds, new[] { 0, 10, 20, 30 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All seconds
        public void Parse_Should_Parse_Seconds_Correctly(string expression, CronFormats formats,
            int[]? expectedSeconds)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedSeconds != null)
            {
                cronExpression.Seconds.Should().BeEquivalentTo(expectedSeconds);
            }
            else
            {
                cronExpression.Seconds.Should().HaveCount(60).And.ContainInOrder(GenerateSequence(0, 59));
            }
        }

        [Theory]
        [InlineData("* */15 * * * *", CronFormats.IncludeSeconds, new[] { 0, 15, 30, 45 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All minutes
        public void Parse_Should_Parse_Minutes_Correctly(string expression, CronFormats formats,
            int[]? expectedMinutes)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedMinutes != null)
            {
                cronExpression.Minutes.Should().BeEquivalentTo(expectedMinutes);
            }
            else
            {
                cronExpression.Minutes.Should().HaveCount(60).And.ContainInOrder(GenerateSequence(0, 59));
            }
        }

        [Theory]
        [InlineData("* * 0-6 * * *", CronFormats.IncludeSeconds, new[] { 0, 1, 2, 3, 4, 5, 6 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All hours
        public void Parse_Should_Parse_Hours_Correctly(string expression, CronFormats formats,
            int[]? expectedHours)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedHours != null)
            {
                cronExpression.Hours.Should().BeEquivalentTo(expectedHours);
            }
            else
            {
                cronExpression.Hours.Should().HaveCount(24).And.ContainInOrder(GenerateSequence(0, 23));
            }
        }

        [Theory]
        [InlineData("* * * * 1-6/2 *", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All months
        [InlineData("* * * * JAN,MAR,MAY *", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })] // Month names
        public void Parse_Should_Parse_Months_Correctly(string expression, CronFormats formats,
            int[]? expectedMonths)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedMonths != null)
            {
                cronExpression.Month.Should().BeEquivalentTo(expectedMonths);
            }
            else
            {
                cronExpression.Month.Should().HaveCount(12).And.ContainInOrder(GenerateSequence(1, 12));
            }
        }

        [Theory]
        [InlineData("* * * * * 1-5", CronFormats.IncludeSeconds, new[] { 1, 2, 3, 4, 5 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All days of week
        [InlineData("* * * * * MON,WED,FRI", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })] // Day names
        public void Parse_Should_Parse_DaysOfWeek_Correctly(string expression, CronFormats formats,
            int[]? expectedDaysOfWeek)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedDaysOfWeek != null)
            {
                cronExpression.DayOfWeek.Should().BeEquivalentTo(expectedDaysOfWeek);
            }
            else
            {
                cronExpression.DayOfWeek.Should().HaveCount(7).And.ContainInOrder(GenerateSequence(0, 6));
            }
        }

        [Theory]
        [InlineData("*/5 * * * * *", CronFormats.IncludeSeconds, "2023-10-01T00:00:00Z", "2023-10-01T00:00:05Z")]
        [InlineData("0 */1 * * * *", CronFormats.IncludeSeconds, "2023-10-01T00:59:59Z", "2023-10-01T01:00:00Z")]
        [InlineData("0 0 * * *", CronFormats.Standard, "2023-10-01T23:59:59Z", "2023-10-02T00:00:00Z")]
        public void GetNextOccurrence_Should_Return_Correct_Next_Occurrence(string expression,
            CronFormats formats, DateTimeOffset baseTime, DateTimeOffset expectedTime)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, formats);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime.UtcDateTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime.UtcDateTime);
        }

        [Theory]
        [InlineData("* * L * *", CronFormats.Standard, "2023-01-30T23:59:59Z",
            "2023-01-31T00:00:00Z")] // Last day of the month
        [InlineData("* * * * 6L", CronFormats.Standard, "2023-01-27T23:59:59Z",
            "2023-01-28T00:00:00Z")] // Last Saturday
        public void GetNextOccurrence_Should_Handle_Special_Operators_Correctly(string expression,
            CronFormats formats,
            DateTimeOffset baseTime, DateTimeOffset expectedTime)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, formats);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime.UtcDateTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime.UtcDateTime);
        }

        [Theory]
        [InlineData("0 0 15W * *", "2023-04-01T00:00:00Z", "2023-04-14T00:00:00Z")]
        [InlineData("0 0 1W * *", "2023-04-01T00:00:00Z", "2023-04-03T00:00:00Z")]
        [InlineData("0 0 31W * *", "2023-03-01T00:00:00Z", "2023-03-31T00:00:00Z")]
        public void GetNextOccurrence_Should_Handle_Nearest_Weekday(string expression, DateTimeOffset baseTime,
            DateTimeOffset expected)
        {
            var cron = CronExpression.Parse(expression, CronFormats.Standard);
            cron.GetNextOccurrence(baseTime.UtcDateTime).Should().Be(expected.UtcDateTime);
        }

        [Theory]
        [InlineData("0 0 L * *", "2023-01-01T00:00:00Z", "2023-01-31T00:00:00Z")]
        [InlineData("0 0 L-0 * *", "2023-01-01T00:00:00Z", "2023-01-31T00:00:00Z")]
        [InlineData("0 0 L-1 * *", "2023-01-01T00:00:00Z", "2023-01-30T00:00:00Z")]
        [InlineData("0 0 L-2 * *", "2023-02-01T00:00:00Z", "2023-02-26T00:00:00Z")]
        [InlineData("0 0 * * 0L", "2023-01-02T00:00:00Z", "2023-01-29T00:00:00Z")]
        [InlineData("0 0 * * 1L", "2023-01-02T00:00:00Z", "2023-01-30T00:00:00Z")]
        [InlineData("0 0 * * 2L", "2023-01-02T00:00:00Z", "2023-01-31T00:00:00Z")]
        [InlineData("0 0 * * 3L", "2023-01-02T00:00:00Z", "2023-01-25T00:00:00Z")]
        [InlineData("0 0 * * 4L", "2023-01-02T00:00:00Z", "2023-01-26T00:00:00Z")]
        [InlineData("0 0 * * 5L", "2023-01-02T00:00:00Z", "2023-01-27T00:00:00Z")]
        [InlineData("0 0 * * 6L", "2023-01-02T00:00:00Z", "2023-01-28T00:00:00Z")]
        [InlineData("0 0 * * L", "2023-01-02T00:00:00Z", "2023-01-07T00:00:00Z")]
        [InlineData("0 0 L 2 *", "2024-02-01T00:00:00Z", "2024-02-29T00:00:00Z")]
        public void GetNextOccurrence_Should_Handle_Last_Day_Operators(string expression, DateTimeOffset baseTime,
            DateTimeOffset expected)
        {
            var cron = CronExpression.Parse(expression, CronFormats.Standard);
            cron.GetNextOccurrence(baseTime.UtcDateTime).Should().Be(expected.UtcDateTime);
        }

        [Theory]
        [InlineData("* * * * * *", "2023-10-01T00:00:00.500Z", "2023-10-01T00:00:01Z")]
        [InlineData("0 * * * * *", "2023-10-01T00:00:00.500Z", "2023-10-01T00:01:00Z")]
        [InlineData("* * * * *", "2023-10-01T00:00:00Z", "2023-10-01T00:01:00Z")]
        [InlineData("0 0 * * *", "2023-10-01T23:59:59Z", "2023-10-02T00:00:00Z")]
        [InlineData("0 0 1 JAN *", "2023-01-01T00:00:00Z", "2024-01-01T00:00:00Z")]
        public void GetNextOccurrence_Should_Return_Strictly_Future_Boundary(string expression,
            DateTimeOffset baseTime, DateTimeOffset expected)
        {
            var format = expression.Split(' ').Length == 6 ? CronFormats.IncludeSeconds : CronFormats.Standard;
            var cron = CronExpression.Parse(expression, format);
            cron.GetNextOccurrence(baseTime.UtcDateTime).Should().Be(expected.UtcDateTime);
        }

        [Fact]
        public void GetNextOccurrence_Should_Return_Null_When_No_Date_Can_Match()
        {
            var cron = CronExpression.Parse("0 0 31 2 *", CronFormats.Standard);
            cron.GetNextOccurrence(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Should().BeNull();
        }

        [Fact]
        public void GetNextOccurrence_Should_Not_Move_Nearest_Weekday_Outside_Short_Month()
        {
            var cron = CronExpression.Parse("0 0 31W 2 *", CronFormats.Standard);
            cron.GetNextOccurrence(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Should().BeNull();
        }

        [Fact]
        public void GetNextOccurrence_Should_Return_Null_At_DateTime_MaxValue()
        {
            var cron = CronExpression.Parse("* * * * * *", CronFormats.IncludeSeconds);
            cron.GetNextOccurrence(DateTime.MaxValue).Should().BeNull();
        }

        [Fact]
        public void Parse_Should_Normalize_Whitespace_Names_And_Single_Start_Steps()
        {
            var cron = CronExpression.Parse("  */15   5/10 * jAn,mAr *  ", CronFormats.Standard);
            cron.Minutes.Should().BeEquivalentTo(new[] { 0, 15, 30, 45 });
            cron.Hours.Should().BeEquivalentTo(new[] { 5, 15 });
            cron.Month.Should().BeEquivalentTo(new[] { 1, 3 });
        }

        [Fact]
        public void Parse_Should_Support_Named_Ranges_And_Deduplicate_Lists()
        {
            var cron = CronExpression.Parse("0 0 * JAN-MAR MON-FRI", CronFormats.Standard);
            cron.Month.Should().BeEquivalentTo(new[] { 1, 2, 3 });
            cron.DayOfWeek.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });

            var duplicated = CronExpression.Parse("0,0,30 0 * * * *", CronFormats.IncludeSeconds);
            duplicated.Seconds.Should().BeEquivalentTo(new[] { 0, 30 });
        }

        [Fact]
        public void GetNextOccurrence_Should_Keep_Day_Of_Month_And_Day_Of_Week_Conjunctive()
        {
            var cron = CronExpression.Parse("0 0 1 * MON", CronFormats.Standard);
            cron.GetNextOccurrence(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .Should().Be(new DateTime(2023, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Theory]
        [InlineData("")]
        [InlineData("* * * *")]
        [InlineData("* * * * * *")]
        [InlineData("61 * * * *")]
        [InlineData("* * * * * 7")]
        [InlineData("* * * * * */0")]
        [InlineData("* * * * 12-1")]
        [InlineData("* * * * 1,,2")]
        [InlineData("* * * * 1--3")]
        [InlineData("* * * * */1/2")]
        [InlineData("* * L-foo * *")]
        [InlineData("* * * * fooL")]
        [InlineData("* * * * JANUARY *")]
        [InlineData("* * * * * 7L")]
        [InlineData("* * * * 32W *")]
        public void Parse_Should_Reject_Invalid_Expressions(string expression)
        {
            Action act = () => CronExpression.Parse(expression, CronFormats.Standard);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Parse_Should_Report_Nth_Weekday_As_Not_Implemented()
        {
            Action act = () => CronExpression.Parse("0 0 * * 2#3", CronFormats.Standard);
            act.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void Parse_Should_Reject_Null_Expression_And_Unsupported_Format()
        {
            Action nullExpression = () => CronExpression.Parse(null!, CronFormats.Standard);
            Action unsupportedFormat = () => CronExpression.Parse("* * * * *", CronFormats.None);
            nullExpression.Should().Throw<ArgumentNullException>();
            unsupportedFormat.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("* * * * * *", CronFormats.Standard)] // Incorrect number of fields
        [InlineData("* * * * * * * *", CronFormats.IncludeSeconds)] // Too many fields
        [InlineData("*/0 * * * * *", CronFormats.IncludeSeconds)] // Invalid step value
        public void Parse_Should_Throw_Exception_On_Invalid_Expressions(string expression, CronFormats formats)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CronExpression.Parse(expression, formats));
        }

        private static int[] GenerateSequence(int start, int end)
        {
            int length = end - start + 1;
            int[] sequence = new int[length];
            for (int i = 0; i < length; i++)
            {
                sequence[i] = start + i;
            }

            return sequence;
        }
    }
}
