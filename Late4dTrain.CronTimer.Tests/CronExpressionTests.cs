using FluentAssertions;

namespace Late4dTrain.CronTimer.Tests
{
    public class CronExpressionTests
    {
        [Theory]
        [InlineData("*/5 * * * * *", CronExpressionType.WithSeconds,
            new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 })]
        [InlineData("0-30/10 * * * * *", CronExpressionType.WithSeconds, new[] { 0, 10, 20, 30 })]
        [InlineData("* * * * * *", CronExpressionType.WithSeconds, null)] // All seconds
        public void Parse_Should_Parse_Seconds_Correctly(string expression, CronExpressionType format,
            int[] expectedSeconds)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* */15 * * * *", CronExpressionType.WithSeconds, new[] { 0, 15, 30, 45 })]
        [InlineData("* * * * * *", CronExpressionType.WithSeconds, null)] // All minutes
        public void Parse_Should_Parse_Minutes_Correctly(string expression, CronExpressionType format,
            int[] expectedMinutes)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * 0-6 * * *", CronExpressionType.WithSeconds, new[] { 0, 1, 2, 3, 4, 5, 6 })]
        [InlineData("* * * * * *", CronExpressionType.WithSeconds, null)] // All hours
        public void Parse_Should_Parse_Hours_Correctly(string expression, CronExpressionType format,
            int[] expectedHours)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * * * 1-6/2 *", CronExpressionType.WithSeconds, new[] { 1, 3, 5 })]
        [InlineData("* * * * * *", CronExpressionType.WithSeconds, null)] // All months
        public void Parse_Should_Parse_Months_Correctly(string expression, CronExpressionType format,
            int[] expectedMonths)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * * * * 1-5", CronExpressionType.WithSeconds, new[] { 1, 2, 3, 4, 5 })]
        [InlineData("* * * * * *", CronExpressionType.WithSeconds, null)] // All days of week
        public void Parse_Should_Parse_DaysOfWeek_Correctly(string expression, CronExpressionType format,
            int[] expectedDaysOfWeek)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("*/5 * * * * *", CronExpressionType.WithSeconds, "2023-10-01T00:00:00Z", "2023-10-01T00:00:05Z")]
        [InlineData("0 */1 * * * *", CronExpressionType.WithSeconds, "2023-10-01T00:59:59Z", "2023-10-01T01:00:00Z")]
        [InlineData("0 0 * * *", CronExpressionType.None, "2023-10-01T23:59:59Z", "2023-10-02T00:00:00Z")]
        public void GetNextOccurrence_Should_Return_Correct_Next_Occurrence(string expression,
            CronExpressionType format, string baseTimeString, string expectedTimeString)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, format);
            var baseTime = DateTime.Parse(baseTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);
            var expectedTime = DateTime.Parse(expectedTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime);
        }

        [Theory]
        [InlineData("* * * 2 *", CronExpressionType.None, "2023-01-31T23:59:59Z", "2023-02-01T00:00:00Z")]
        [InlineData("0 0 29 2 *", CronExpressionType.None, "2023-02-28T23:59:59Z", "2024-02-29T00:00:00Z")] // Non-leap year
        [InlineData("0 0 29 2 *", CronExpressionType.None, "2024-02-28T23:59:59Z", "2024-02-29T00:00:00Z")] // Leap year
        public void GetNextOccurrence_Should_Handle_Month_End_Correctly(string expression, CronExpressionType format,
            string baseTimeString, string expectedTimeString)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, format);
            var baseTime = DateTime.Parse(baseTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);
            DateTime? expectedTime = string.IsNullOrEmpty(expectedTimeString)
                ? (DateTime?)null
                : DateTime.Parse(expectedTimeString, null,
                    System.Globalization.DateTimeStyles.AssumeUniversal |
                    System.Globalization.DateTimeStyles.AdjustToUniversal);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime);
        }

        private int[] GenerateSequence(int start, int end)
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
