namespace Late4dTrain.CronTimer.Tests
{
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Xunit;

    public class ParserSpike
    {
        [Theory]
        [InlineData("* * * * *", "Every minute")]
        [InlineData("5 * * * *", "At minute 5")]
        [InlineData("6 * * * *", "At minute 6")]
        [InlineData("1 * * * *", "At minute 1")]
        [InlineData("59 * * * *", "At minute 59")]
        [InlineData("60 * * * *", "Didn't match at all")]
        [InlineData("600 * * * *", "Didn't match at all")]
        [InlineData("a * * * *", "Didn't match at all")]
        [InlineData("5-10 * * * *", "At every minute from 5 through 10")]
        public void ParseMinuteSpikeTest(string input, string expected)
        {
            var schedule = input.Split(' ');
            var minute = schedule[0];
            var result = ParseMinute(minute);
            result.Should().Be(expected);
        }

        private string ParseMinute(string schedule) =>
            schedule switch
            {
                var s when R(s, @"[6][0-9]", out _) => "Didn't match at all",
                var s when R(s, @"[1-5]?[0-9]", out var atMinute) => $"At minute {atMinute.Captures[0].Value}",
                var s when R(s, @"[*]", out _) => "Every minute",
                _ => "Didn't match at all"
            };

        private bool R(string input, string pattern, out Match match)
            => (match = Regex.Match(input, pattern)).Success;
    }
}
