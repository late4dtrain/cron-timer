using FluentAssertions;
using Late4dTrain.CronTimer.Parser;
using Xunit;

namespace Late4dTrain.CronTimer.Tests;

public class CronTabExtensionsTests
{
    [Theory]
    [InlineData("* * * * * *", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
    public void ValidateExpression_Should_Throw_For_Invalid_Expressions(string expression, CronFormats formats, string expectedMessage)
    {
        // Act
        Action act = () => new CronTab(expression, formats);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("* * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("0 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("0-0 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("0-59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("59-59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("*/1 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("*/60 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("*/59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("0,59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("0-59/59 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("10,20,30,40,50 * * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 0 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 0-0 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 0-59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 59-59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* */1 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* */59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 0,59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 0-59/59 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* 10,20,30,40,50 * * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 0 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 0-0 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 0-23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 23-23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 0-23/23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 10,20,23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * */0 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * */23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * 0,23 * * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 1 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 31 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 1-1 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 1-31 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 31-31 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 1-31/1 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 1-31/31 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * 10,20,30 * *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 1 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * JAN *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 12 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 1-1 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * JAN-JAN *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 1-12 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 12-12 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 1-12/1 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 1-12/12 *", CronFormats.IncludeSeconds)]
    [InlineData("* * * * 10,11,12 *", CronFormats.IncludeSeconds)]
    public void ValidateExpression_Should_Not_Throw_For_Valid_Expressions(string expression, CronFormats formats)
    {
        // Act
        Action act = () => new CronTab(expression, formats);

        // Assert
        act.Should().NotThrow();
    }
}
