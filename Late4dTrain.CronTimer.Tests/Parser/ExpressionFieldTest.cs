using FluentAssertions;
using Late4dTrain.CronTimer.Parser;

namespace Late4dTrain.CronTimer.Tests.Parser;

public class CronTabExpressionFieldTests
{
    [Fact]
    public void Parse_SingleValue_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "5";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(1);

        var data = field.Data[0];
        data.Start.Should().Be("5");
        data.End.Should().BeEmpty();
        data.Step.Should().BeEmpty();
        data.IsRange.Should().BeFalse();
        data.HasStep.Should().BeFalse();
    }

    [Fact]
    public void Parse_Range_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1-5";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(1);

        var data = field.Data[0];
        data.Start.Should().Be("1");
        data.End.Should().Be("5");
        data.Step.Should().BeEmpty();
        data.IsRange.Should().BeTrue();
        data.HasStep.Should().BeFalse();
    }

    [Fact]
    public void Parse_List_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1,2,3";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(3);

        field.Data[0].Start.Should().Be("1");
        field.Data[0].End.Should().BeEmpty();
        field.Data[0].Step.Should().BeEmpty();
        field.Data[0].IsRange.Should().BeFalse();
        field.Data[0].HasStep.Should().BeFalse();

        field.Data[1].Start.Should().Be("2");
        field.Data[2].Start.Should().Be("3");
    }

    [Fact]
    public void Parse_Step_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "*/5";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(1);

        var data = field.Data[0];
        data.Start.Should().Be("*");
        data.End.Should().BeEmpty();
        data.Step.Should().Be("5");
        data.IsRange.Should().BeFalse();
        data.HasStep.Should().BeTrue();
    }

    [Fact]
    public void Parse_RangeWithStep_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1-5/2";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(1);

        var data = field.Data[0];
        data.Start.Should().Be("1");
        data.End.Should().Be("5");
        data.Step.Should().Be("2");
        data.IsRange.Should().BeTrue();
        data.HasStep.Should().BeTrue();
    }

    [Fact]
    public void Parse_ListWithStep_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1,2,3/2";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(3);

        field.Data[0].Start.Should().Be("1");
        field.Data[1].Start.Should().Be("2");
        field.Data[2].Start.Should().Be("3");
        field.Data[2].Step.Should().Be("2");
        field.Data[2].HasStep.Should().BeTrue();
        field.Data[2].IsRange.Should().BeFalse();
    }

    [Fact]
    public void Parse_MixedList_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1-5,7,9/2";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(3);

        // First part: Range without step
        var data1 = field.Data[0];
        data1.Start.Should().Be("1");
        data1.End.Should().Be("5");
        data1.Step.Should().BeEmpty();
        data1.IsRange.Should().BeTrue();
        data1.HasStep.Should().BeFalse();

        // Second part: Single value without step
        var data2 = field.Data[1];
        data2.Start.Should().Be("7");
        data2.End.Should().BeEmpty();
        data2.Step.Should().BeEmpty();
        data2.IsRange.Should().BeFalse();
        data2.HasStep.Should().BeFalse();

        // Third part: Single value with step
        var data3 = field.Data[2];
        data3.Start.Should().Be("9");
        data3.End.Should().BeEmpty();
        data3.Step.Should().Be("2");
        data3.IsRange.Should().BeFalse();
        data3.HasStep.Should().BeTrue();
    }

    [Fact]
    public void Parse_RangeWithStepAndList_ShouldHaveCorrectData()
    {
        // Arrange
        var expression = "1-5/2,6,8-10";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(3);

        // First part: Range with step
        var data1 = field.Data[0];
        data1.Start.Should().Be("1");
        data1.End.Should().Be("5");
        data1.Step.Should().Be("2");
        data1.IsRange.Should().BeTrue();
        data1.HasStep.Should().BeTrue();

        // Second part: Single value without step
        var data2 = field.Data[1];
        data2.Start.Should().Be("6");
        data2.End.Should().BeEmpty();
        data2.Step.Should().BeEmpty();
        data2.IsRange.Should().BeFalse();
        data2.HasStep.Should().BeFalse();

        // Third part: Range without step
        var data3 = field.Data[2];
        data3.Start.Should().Be("8");
        data3.End.Should().Be("10");
        data3.Step.Should().BeEmpty();
        data3.IsRange.Should().BeTrue();
        data3.HasStep.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidExpression_ShouldHandleGracefully()
    {
        // Arrange
        var expression = "invalid";

        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().HaveCount(1);

        var data = field.Data[0];
        data.Start.Should().Be("invalid");
        data.End.Should().BeEmpty();
        data.Step.Should().BeEmpty();
        data.IsRange.Should().BeFalse();
        data.HasStep.Should().BeFalse();
    }

    [Theory]
    [InlineData("*/15", "*", "", "15", false, true)]
    [InlineData("0-59/2", "0", "59", "2", true, true)]
    [InlineData("0,15,30,45", "0", "", "", false, false)]
    public void Parse_VariousExpressions_ShouldMatchExpected(
        string expression,
        string expectedStart,
        string expectedEnd,
        string expectedStep,
        bool expectedIsRange,
        bool expectedHasStep)
    {
        // Act
        var field = new CronTabExpressionField(expression);

        // Assert
        field.Data.Should().NotBeEmpty();
        var data = field.Data[0];
        data.Start.Should().Be(expectedStart);
        data.End.Should().Be(expectedEnd);
        data.Step.Should().Be(expectedStep);
        data.IsRange.Should().Be(expectedIsRange);
        data.HasStep.Should().Be(expectedHasStep);
    }
}
