using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Late4dTrain.CronTimer.Parser
{
    public static class CronTabExtensions
    {
        // Static regex patterns for reuse
        // General Patterns
        private static readonly Regex AsteriskRegex = new(@"^\*$", RegexOptions.Compiled);
        private static readonly Regex AsteriskWithSlashRegex = new(@"^\*\/\d+$", RegexOptions.Compiled);
        private static readonly Regex SingleNumberRegex = new(@"^\d{1,2}$", RegexOptions.Compiled);
        private static readonly Regex NumberRangeRegex = new(@"^\d{1,2}-\d{1,2}$", RegexOptions.Compiled);
        private static readonly Regex NumberWithSlashRegex = new(@"^\d{1,2}\/\d+$", RegexOptions.Compiled);

        private static readonly Regex NumberRangeWithSlashRegex =
            new Regex(@"^\d{1,2}-\d{1,2}/\d+$", RegexOptions.Compiled);

        private static readonly Regex LRegex = new(@"^L$", RegexOptions.Compiled);

        // Month Patterns
        private static readonly Regex MonthNameRegex = new Regex(@"^(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MonthNameRangeRegex =
            new Regex(
                @"^(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)-(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MonthNameListRegex =
            new Regex(
                @"^((JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC),)+(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] ValidMonths =
            { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        // Day of Week Patterns
        private static readonly Regex DayOfWeekNameRegex = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

        private static readonly Regex DayOfWeekNameRangeRegex = new(@"^[A-Z]{3}-[A-Z]{3}$", RegexOptions.Compiled);

        private static readonly Regex DayOfWeekNameListRegex = new(@"^([A-Z]{3},)+[A-Z]{3}$", RegexOptions.Compiled);

        private static readonly Regex NumberWithLRegex = new(@"^\d{1}L$", RegexOptions.Compiled);
        private static readonly Regex NumberWithHashRegex = new(@"^\d{1}#\d{1}$", RegexOptions.Compiled);
        private static readonly string[] ValidDays = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

        public static void ValidateExpression(this CronTab cronTab)
        {
            var fields = cronTab.Expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var expectedFieldCount = cronTab.Formats.HasFlagFast(CronFormats.IncludeSeconds) ? 6 : 5;

            if (fields.Length != expectedFieldCount)
            {
                throw new ArgumentException(
                    $"Invalid number of fields in cron expression. Expected {expectedFieldCount}, got {fields.Length}.");
            }

            var fieldIndex = 0;

            if (cronTab.Formats.HasFlagFast(CronFormats.IncludeSeconds))
            {
                fields[fieldIndex++].ValidateField(0, 59, "seconds");
            }

            fields[fieldIndex++].ValidateField(0, 59, "minutes");
            fields[fieldIndex++].ValidateField(0, 23, "hours");
            fields[fieldIndex++].ValidateField(1, 31, "day of month");
            fields[fieldIndex++].ValidateField(1, 12, "month");
            fields[fieldIndex].ValidateDayOfWeekField();
        }

        public static void ValidateField(this string field, int minValue, int maxValue, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException($"The {fieldName} field is empty.");

            if (AsteriskRegex.IsMatch(field) || AsteriskWithSlashRegex.IsMatch(field))
                return;

            var parts = field.Split(',');

            foreach (var part in parts)
            {
                switch (part)
                {
                    case var _ when AsteriskRegex.IsMatch(part):
                        continue;
                    case var _ when NumberRangeWithSlashRegex.IsMatch(part):
                        /// Handle patterns like "0-59/59"
                        var numberRangeWithSlash = part.Split('/');
                        if (numberRangeWithSlash.Length != 2)
                            throw new ArgumentException($"Invalid step value in {fieldName} field.");

                        var rangePart = numberRangeWithSlash[0].Split('-');

                        /// Validate the range part
                        if (!int.TryParse(rangePart[0], out var start) || !int.TryParse(rangePart[1], out var end) ||
                            !int.TryParse(numberRangeWithSlash[1], out var slashValue))
                            throw new ArgumentException($"Invalid values in {fieldName} field.");

                        $"{start}".ValidateRangeOrValue(minValue, maxValue, fieldName);
                        $"{end}".ValidateRangeOrValue(minValue, maxValue, fieldName);
                        $"{slashValue}".ValidateRangeOrValue(minValue, maxValue, fieldName);
                        break;
                    case var _ when NumberWithSlashRegex.IsMatch(part):
                        // Existing code for patterns like "0/5"
                        var numberWithSlash = part.Split('/');
                        if (numberWithSlash.Length != 2)
                            throw new ArgumentException($"Invalid step value in {fieldName} field.");
                        /// Validate the value part
                        if (!int.TryParse(numberWithSlash[0], out var value))
                            throw new ArgumentException($"Invalid value '{part}' in {fieldName} field.");
                        /// Validate the step value
                        if (!int.TryParse(numberWithSlash[1], out var step) || step <= 0)
                            throw new ArgumentException(
                                $"Invalid step value in {fieldName} field. Step must be greater than zero.");
                        break;
                    case var _ when NumberRangeRegex.IsMatch(part) || SingleNumberRegex.IsMatch(part):
                        /// Existing code for ranges and single numbers
                        part.ValidateRangeOrValue(minValue, maxValue, fieldName);
                        break;
                    case var _ when MonthNameRegex.IsMatch(part):
                        break;
                    case var _ when MonthNameRangeRegex.IsMatch(part):
                        break;
                    default:
                        throw new ArgumentException($"Invalid {fieldName} field format.");
                }
            }
        }

        public static void ValidateRangeOrValue(this string part, int minValue, int maxValue, string fieldName)
        {
            if (NumberRangeRegex.IsMatch(part))
            {
                var rangeParts = part.Split('-');
                if (!int.TryParse(rangeParts[0], out var start) || !int.TryParse(rangeParts[1], out var end))
                    throw new ArgumentException($"Invalid range in {fieldName} field.");

                if (start < minValue || start > maxValue || end < minValue || end > maxValue || start > end)
                    throw new ArgumentException(
                        $"Invalid range in {fieldName} field. Range values must be between {minValue} and {maxValue}.");
            }
            else if (SingleNumberRegex.IsMatch(part))
            {
                if (!int.TryParse(part, out var value) || value < minValue || value > maxValue)
                    throw new ArgumentException(
                        $"Invalid value '{part}' in {fieldName} field. Value must be between {minValue} and {maxValue}.");
            }
            else
            {
                throw new ArgumentException($"Invalid {fieldName} field format.");
            }
        }

        public static void ValidateMonthField(this string field)
        {
            var parts = field.Split(',');

            foreach (var part in parts)
            {
                if (MonthNameRangeRegex.IsMatch(part))
                {
                    var rangeParts = part.Split('-');
                    if (!ValidMonths.Contains(rangeParts[0].ToUpper()) ||
                        !ValidMonths.Contains(rangeParts[1].ToUpper()))
                    {
                        throw new ArgumentException($"Invalid month range '{part}'.");
                    }
                }
                else if (SingleNumberRegex.IsMatch(part) || MonthNameRegex.IsMatch(part))
                {
                    part.ValidateMonthValue();
                }
                else
                {
                    throw new ArgumentException($"Invalid month field format: '{part}'.");
                }
            }
        }

        public static void ValidateMonthValue(this string value)
        {
            if (ValidMonths.Contains(value.ToUpper()))
            {
                // Valid month abbreviation
            }
            else
            {
                throw new ArgumentException($"Invalid month value '{value}'.");
            }
        }

        public static void ValidateDayOfWeekField(this string field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("The day of week field is empty.");
            }

            var parts = field.Split(',');

            foreach (var part in parts)
            {
                if (AsteriskRegex.IsMatch(part) || LRegex.IsMatch(part))
                {
                    continue;
                }
                else if (NumberWithHashRegex.IsMatch(part))
                {
                    var hashParts = part.Split('#');
                    if (hashParts.Length != 2 ||
                        !int.TryParse(hashParts[0], out var dayOfWeek) || dayOfWeek < 0 || dayOfWeek > 7 ||
                        !int.TryParse(hashParts[1], out var nth) || nth < 1 || nth > 5)
                    {
                        throw new ArgumentException("Invalid '#' usage in day of week field.");
                    }
                }
                else if (NumberWithLRegex.IsMatch(part))
                {
                    var dayPart = part.Substring(0, part.Length - 1);
                    if (!int.TryParse(dayPart, out var dayOfWeek) || dayOfWeek < 0 || dayOfWeek > 7)
                    {
                        throw new ArgumentException("Invalid 'L' usage in day of week field.");
                    }
                }
                else if (NumberRangeRegex.IsMatch(part) || SingleNumberRegex.IsMatch(part))
                {
                    part.ValidateDayOfWeekValue();
                }
                else if (DayOfWeekNameRegex.IsMatch(part))
                {
                    part.ValidateDayOfWeekValue();
                }
                else if (DayOfWeekNameRangeRegex.IsMatch(part))
                {
                    var rangeParts = part.Split('-');
                    rangeParts[0].ValidateDayOfWeekValue();
                    rangeParts[1].ValidateDayOfWeekValue();
                }
                else
                {
                    throw new ArgumentException($"Invalid day of week field format: '{part}'.");
                }
            }
        }

        public static void ValidateDayOfWeekValue(this string value)
        {
            if (int.TryParse(value, out var dayOfWeek))
            {
                if (dayOfWeek < 0 || dayOfWeek > 7)
                    throw new ArgumentException(
                        $"Invalid day of week value '{dayOfWeek}'. Day of week must be between 0 and 7.");
            }
            else if (ValidDays.Contains(value.ToUpper()))
            {
                // Valid day abbreviation
            }
            else
            {
                throw new ArgumentException($"Invalid day of week value '{value}'.");
            }
        }
    }
}
