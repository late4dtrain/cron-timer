using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Late4dTrain.CronTimer.Parser
{
    public class CronExpression
    {
        public SortedSet<int> Seconds { get; private set; }
        public SortedSet<int> Minutes { get; private set; }
        public SortedSet<int> Hours { get; private set; }
        public SortedSet<int> DayOfMonth { get; private set; }
        public SortedSet<int> Month { get; private set; }
        public SortedSet<int> DayOfWeek { get; private set; }

        // Flags indicating special operators usage
        public bool HasDayOfMonthSpecialCharacters { get; private set; }
        public bool HasDayOfWeekSpecialCharacters { get; private set; }

        public static CronExpression Parse(string expression, CronFormats formats)
        {
            string[] parts = expression.Split(' ');

            int expectedFieldCount = formats == CronFormats.IncludeSeconds ? 6 : 5;

            if (parts.Length != expectedFieldCount)
                throw new ArgumentException(
                    $"Invalid cron expression: '{expression}'. Expected {expectedFieldCount} fields but got {parts.Length}.");

            var cron = new CronExpression();

            int index = 0;

            if (formats == CronFormats.IncludeSeconds)
            {
                cron.Seconds = ParseField(parts[index++], 0, 59, "Seconds");
            }
            else
            {
                cron.Seconds = new SortedSet<int> { 0 }; // default to 0 seconds
            }

            cron.Minutes = ParseField(parts[index++], 0, 59, "Minutes");
            cron.Hours = ParseField(parts[index++], 0, 23, "Hours");
            cron.DayOfMonth = ParseDayOfMonthField(parts[index++], cron);
            cron.Month = ParseField(parts[index++], 1, 12, "Month", monthNames: true);
            cron.DayOfWeek = ParseDayOfWeekField(parts[index], cron);

            return cron;
        }

        private static SortedSet<int> ParseField(string field, int minValue, int maxValue, string fieldName,
            bool monthNames = false, bool dayOfWeekNames = false)
        {
            var values = new SortedSet<int>();
            field = field.Trim();

            if (field == "*")
            {
                values.UnionWith(Enumerable.Range(minValue, maxValue - minValue + 1));
                return values;
            }

            var parts = field.Split(',');
            foreach (var part in parts)
            {
                if (part.Contains("/"))
                {
                    ParseStep(part, minValue, maxValue, values, fieldName, monthNames, dayOfWeekNames);
                }
                else if (part.Contains("-"))
                {
                    ParseRange(part, minValue, maxValue, values, fieldName, monthNames, dayOfWeekNames);
                }
                else
                {
                    ParseValue(part, minValue, maxValue, values, fieldName, monthNames, dayOfWeekNames);
                }
            }

            return values;
        }

        private static void ParseStep(string part, int minValue, int maxValue, SortedSet<int> values, string fieldName,
            bool monthNames, bool dayOfWeekNames)
        {
            var stepParts = part.Split('/');
            if (stepParts.Length != 2)
                throw new ArgumentException($"Invalid step value in cron field '{fieldName}': {part}");

            var rangePart = stepParts[0];
            if (!int.TryParse(stepParts[1], out int step) || step <= 0)
                throw new ArgumentException($"Invalid step value in cron field '{fieldName}': {part}");

            int rangeStart = minValue;
            int rangeEnd = maxValue;

            if (!string.IsNullOrEmpty(rangePart) && rangePart != "*")
            {
                if (rangePart.Contains("-"))
                {
                    ParseRangeBounds(rangePart, minValue, maxValue, fieldName, monthNames, dayOfWeekNames,
                        out rangeStart, out rangeEnd);
                }
                else
                {
                    rangeStart = ParseSingleValue(rangePart, minValue, maxValue, fieldName, monthNames, dayOfWeekNames);
                    rangeEnd = rangeStart;
                }
            }

            for (int i = rangeStart; i <= rangeEnd; i += step)
            {
                if (i >= minValue && i <= maxValue)
                    values.Add(i);
            }
        }

        [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Method is used for parsing cron expressions.")]
        private static void ParseRangeBounds(
            string part,
            int minValue,
            int maxValue,
            string fieldName,
            bool monthNames,
            bool dayOfWeekNames,
            out int rangeStart,
            out int rangeEnd)
        {
            var rangeBounds = part.Split('-');
            if (rangeBounds.Length != 2)
                throw new ArgumentException($"Invalid range in cron field '{fieldName}': {part}");

            rangeStart = ParseSingleValue(rangeBounds[0], minValue, maxValue, fieldName, monthNames, dayOfWeekNames);
            rangeEnd = ParseSingleValue(rangeBounds[1], minValue, maxValue, fieldName, monthNames, dayOfWeekNames);

            if (rangeStart > rangeEnd)
                throw new ArgumentException($"Range start greater than range end in cron field '{fieldName}': {part}");
        }

        private static void ParseRange(string part, int minValue, int maxValue, SortedSet<int> values, string fieldName,
            bool monthNames, bool dayOfWeekNames)
        {
            ParseRangeBounds(part, minValue, maxValue, fieldName, monthNames, dayOfWeekNames, out int rangeStart,
                out int rangeEnd);

            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                values.Add(i);
            }
        }

        private static void ParseValue(string part, int minValue, int maxValue, SortedSet<int> values, string fieldName,
            bool monthNames, bool dayOfWeekNames)
        {
            int val = ParseSingleValue(part, minValue, maxValue, fieldName, monthNames, dayOfWeekNames);
            values.Add(val);
        }

        private static int ParseSingleValue(string value, int minValue, int maxValue, string fieldName, bool monthNames,
            bool dayOfWeekNames)
        {
            value = value.Trim();
            int val;

            if (monthNames && TryParseMonthName(value, out val))
            {
                return val;
            }

            if (dayOfWeekNames && TryParseDayOfWeekName(value, out val))
            {
                return val;
            }

            if (!int.TryParse(value, out val))
                throw new ArgumentException($"Invalid value in cron field '{fieldName}': {value}");

            if (val < minValue || val > maxValue)
                throw new ArgumentException(
                    $"Value {val} out of range in cron field '{fieldName}'. Expected {minValue}-{maxValue}.");

            return val;
        }

        private static bool TryParseMonthName(string value, out int month)
        {
            month = 0;
            var monthNames = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

            for (int i = 0; i < monthNames.Length - 1; i++)
            {
                if (monthNames[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    month = i + 1;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseDayOfWeekName(string value, out int dayOfWeek)
        {
            dayOfWeek = 0;
            var dayNames = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames;

            for (int i = 0; i < dayNames.Length; i++)
            {
                if (dayNames[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    dayOfWeek = i;
                    return true;
                }
            }

            return false;
        }

        [SuppressMessage("Major Code Smell", "S3776: Cognitive Complexity of methods should not be too high", Justification = "Method is used for parsing cron expressions.")]
        private static SortedSet<int> ParseDayOfMonthField(string field, CronExpression cron)
        {
            var values = new SortedSet<int>();
            field = field.Trim();

            if (field.Contains("L"))
            {
                cron.HasDayOfMonthSpecialCharacters = true;

                if (field == "L")
                {
                    // Last day of the month
                    values.Add(-1); // Use -1 as a marker for 'L'
                }
                else if (field.StartsWith("L-"))
                {
                    // e.g., 'L-3' for third to last day of the month
                    if (int.TryParse(field.Substring(2), out int offset) && offset >= 0)
                    {
                        values.Add(-offset); // Use negative numbers to represent 'L-offset'
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid 'L' offset in cron field 'DayOfMonth': {field}");
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid 'L' usage in cron field 'DayOfMonth': {field}");
                }
            }
            else if (field.Contains("W"))
            {
                // Handle 'W' operator for nearest weekday
                cron.HasDayOfMonthSpecialCharacters = true;
                if (int.TryParse(field.TrimEnd('W'), out int day))
                {
                    if (day >= 1 && day <= 31)
                    {
                        values.Add(day); // Mark the day for 'W' processing
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid day value for 'W' in cron field 'DayOfMonth': {field}");
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid 'W' usage in cron field 'DayOfMonth': {field}");
                }
            }
            else
            {
                values = ParseField(field, 1, 31, "DayOfMonth");
            }

            return values;
        }

        private static SortedSet<int> ParseDayOfWeekField(string field, CronExpression cron)
        {
            var values = new SortedSet<int>();
            field = field.Trim();

            if (field.Contains("#"))
            {
                // Handle 'nth' occurrence of a weekday in a month (e.g., '3#2' for the second Tuesday)
                cron.HasDayOfWeekSpecialCharacters = true;
                // Parsing logic for '#' operator would be implemented here
                throw new NotImplementedException("The '#' operator is not implemented yet.");
            }
            else if (field.Contains("L"))
            {
                // Handle 'L' operator for last day of the week
                cron.HasDayOfWeekSpecialCharacters = true;
                if (field.Length == 1 && field == "L")
                {
                    values.Add(-1); // Use -1 as a marker for 'L' in day of week
                }
                else if (int.TryParse(field.TrimEnd('L'), out int dayOfWeek) && dayOfWeek >= 0 && dayOfWeek <= 6)
                {
                    values.Add(-dayOfWeek); // Use negative numbers to represent 'dayOfWeekL'
                }
                else
                {
                    throw new ArgumentException($"Invalid 'L' usage in cron field 'DayOfWeek': {field}");
                }
            }
            else
            {
                values = ParseField(field, 0, 6, "DayOfWeek", dayOfWeekNames: true);
            }

            return values;
        }

        // The rest of the class remains the same, including the GetNextOccurrence method
        // and any other necessary methods for occurrence calculation.
        // For brevity, I will include the optimized GetNextOccurrence method from earlier.

        [SuppressMessage("Major Code Smell", "S3776: Cognitive Complexity of methods should not be too high", Justification = "Method is used for parsing cron expressions.")]
        public DateTime? GetNextOccurrence(DateTime baseTime)
        {
            DateTime next = baseTime;

            if (Seconds != null && Seconds.Count > 0 && !(Seconds.Count == 1 && Seconds.Contains(0)))
            {
                next = next.AddSeconds(1);
            }
            else
            {
                next = next.AddMinutes(1);
                next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, next.Kind);
            }

            for (int i = 0; i < 100000; i++) // Safety limit to prevent infinite loops
            {
                if (!Month.Contains(next.Month))
                {
                    // Move to the next valid month
                    int nextMonth = GetNextValue(Month, next.Month);
                    int yearsToAdd = nextMonth <= next.Month ? 1 : 0;
                    next = new DateTime(next.Year + yearsToAdd, nextMonth, 1, 0, 0, 0, next.Kind);
                    continue;
                }

                // Handle DayOfMonth special characters
                if (HasDayOfMonthSpecialCharacters)
                {
                    if (!IsValidDayOfMonth(next))
                    {
                        next = next.AddDays(1);
                        next = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, next.Kind);
                        continue;
                    }
                }
                else if (!DayOfMonth.Contains(next.Day))
                {
                    // Move to next valid day
                    next = next.AddDays(1);
                    next = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, next.Kind);
                    continue;
                }

                // Handle DayOfWeek special characters
                if (HasDayOfWeekSpecialCharacters)
                {
                    if (!IsValidDayOfWeek(next))
                    {
                        next = next.AddDays(1);
                        next = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, next.Kind);
                        continue;
                    }
                }
                else if (!DayOfWeek.Contains((int)next.DayOfWeek))
                {
                    // Move to next valid day
                    next = next.AddDays(1);
                    next = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, next.Kind);
                    continue;
                }

                if (!Hours.Contains(next.Hour))
                {
                    // Move to the next valid hour
                    int nextHour = GetNextValue(Hours, next.Hour);
                    if (nextHour <= next.Hour)
                    {
                        next = next.AddDays(1);
                    }

                    next = new DateTime(next.Year, next.Month, next.Day, nextHour, 0, 0, next.Kind);
                    continue;
                }

                if (!Minutes.Contains(next.Minute))
                {
                    // Move to the next valid minute
                    int nextMinute = GetNextValue(Minutes, next.Minute);
                    if (nextMinute <= next.Minute)
                    {
                        next = next.AddHours(1);
                        next = new DateTime(next.Year, next.Month, next.Day, next.Hour, 0, 0, next.Kind);
                        continue;
                    }

                    next = new DateTime(next.Year, next.Month, next.Day, next.Hour, nextMinute, 0, next.Kind);
                    continue;
                }

                if (Seconds != null && Seconds.Count > 0)
                {
                    if (!Seconds.Contains(next.Second))
                    {
                        // Move to the next valid second
                        int nextSecond = GetNextValue(Seconds, next.Second);
                        if (nextSecond <= next.Second)
                        {
                            next = next.AddMinutes(1);
                            next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, next.Kind);
                            continue;
                        }

                        next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, nextSecond,
                            next.Kind);
                        continue;
                    }
                }
                else
                {
                    next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, next.Kind);
                }

                // All fields match
                return next;
            }

            // If no occurrence is found within a reasonable limit, return null
            return null;
        }

        private bool IsValidDayOfMonth(DateTime date)
        {
            // Handle 'L' operator in DayOfMonth
            foreach (var value in DayOfMonth)
            {
                if (value == -1)
                {
                    int lastDay = DateTime.DaysInMonth(date.Year, date.Month);
                    if (date.Day == lastDay)
                        return true;
                }
                else if (value < 0)
                {
                    int offset = -value;
                    int lastDay = DateTime.DaysInMonth(date.Year, date.Month);
                    if (date.Day == lastDay - offset)
                        return true;
                }
                else
                {
                    if (date.Day == value)
                        return true;
                }
            }

            return false;
        }

        private bool IsValidDayOfWeek(DateTime date)
        {
            // Handle 'L' operator in DayOfWeek
            foreach (var value in DayOfWeek)
            {
                if (value == -1)
                {
                    // Last day of the week (Saturday)
                    if ((int)date.DayOfWeek == 6)
                        return true;
                }
                else if (value < 0)
                {
                    int dayOfWeek = -value;
                    // Last occurrence of the dayOfWeek in the month
                    if (IsLastDayOfWeekInMonth(date, dayOfWeek))
                        return true;
                }
                else
                {
                    if ((int)date.DayOfWeek == value)
                        return true;
                }
            }

            return false;
        }

        private static bool IsLastDayOfWeekInMonth(DateTime date, int dayOfWeek)
        {
            if ((int)date.DayOfWeek != dayOfWeek)
                return false;

            DateTime nextWeekSameDay = date.AddDays(7);
            return nextWeekSameDay.Month != date.Month;
        }

        private static int GetNextValue(SortedSet<int> values, int currentValue)
        {
            // Try to find the next higher value
            foreach (var val in values)
            {
                if (val > currentValue)
                    return val;
            }

            // If not found, wrap around to the first value
            return values.Min;
        }
    }
}
