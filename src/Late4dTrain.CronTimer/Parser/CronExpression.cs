using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Late4dTrain.CronTimer.Parser
{
    public class CronExpression
    {
        private enum DayOfMonthSpecialKind { None, LastDayOffset, NearestWeekday }
        private enum DayOfWeekSpecialKind { None, LastSaturday, LastOccurrence }

        private DayOfMonthSpecialKind _dayOfMonthSpecialKind;
        private int _dayOfMonthSpecialValue;
        private DayOfWeekSpecialKind _dayOfWeekSpecialKind;
        private int _dayOfWeekSpecialValue;

        public SortedSet<int> Seconds { get; private set; }
        public SortedSet<int> Minutes { get; private set; }
        public SortedSet<int> Hours { get; private set; }
        public SortedSet<int> DayOfMonth { get; private set; }
        public SortedSet<int> Month { get; private set; }
        public SortedSet<int> DayOfWeek { get; private set; }
        public bool HasDayOfMonthSpecialCharacters => _dayOfMonthSpecialKind != DayOfMonthSpecialKind.None;
        public bool HasDayOfWeekSpecialCharacters => _dayOfWeekSpecialKind != DayOfWeekSpecialKind.None;

        public static CronExpression Parse(string expression, CronFormats formats)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));
            if (formats != CronFormats.Standard && formats != CronFormats.IncludeSeconds)
                throw new ArgumentOutOfRangeException(nameof(formats), formats, "A single supported cron format is required.");

            var parts = expression.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var expectedFields = formats == CronFormats.IncludeSeconds ? 6 : 5;
            if (parts.Length != expectedFields)
                throw new ArgumentException(
                    $"Invalid cron expression: '{expression}'. Expected {expectedFields} fields but got {parts.Length}.",
                    nameof(expression));

            var result = new CronExpression();
            var index = 0;
            result.Seconds = formats == CronFormats.IncludeSeconds
                ? ParseField(parts[index++], 0, 59, "Seconds")
                : new SortedSet<int> { 0 };
            result.Minutes = ParseField(parts[index++], 0, 59, "Minutes");
            result.Hours = ParseField(parts[index++], 0, 23, "Hours");
            result.DayOfMonth = ParseDayOfMonth(parts[index++], result);
            result.Month = ParseField(parts[index++], 1, 12, "Month", monthNames: true);
            result.DayOfWeek = ParseDayOfWeek(parts[index], result);
            return result;
        }

        private static SortedSet<int> ParseField(string field, int min, int max, string name,
            bool monthNames = false, bool dayNames = false)
        {
            var result = new SortedSet<int>();
            foreach (var rawItem in field.Split(','))
            {
                var item = rawItem.Trim();
                if (item.Length == 0)
                    throw Invalid(name, item);

                var stepParts = item.Split('/');
                if (stepParts.Length > 2)
                    throw Invalid(name, item);
                if (stepParts.Length == 2)
                {
                    if (!int.TryParse(stepParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var step) || step <= 0)
                        throw Invalid(name, item);

                    var start = min;
                    var end = max;
                    var range = stepParts[0];
                    if (range.Length != 0 && range != "*")
                    {
                        if (range.Contains("-"))
                            (start, end) = ParseRange(range, min, max, name, monthNames, dayNames);
                        else
                        {
                            start = ParseValue(range, min, max, name, monthNames, dayNames);
                        }
                    }
                    for (var value = start; value <= end; value += step)
                        result.Add(value);
                    continue;
                }

                if (item == "*")
                {
                    for (var value = min; value <= max; value++) result.Add(value);
                }
                else if (item.Contains("-"))
                {
                    var (start, end) = ParseRange(item, min, max, name, monthNames, dayNames);
                    for (var value = start; value <= end; value++) result.Add(value);
                }
                else
                {
                    result.Add(ParseValue(item, min, max, name, monthNames, dayNames));
                }
            }

            if (result.Count == 0)
                throw Invalid(name, field);
            return result;
        }

        private static (int start, int end) ParseRange(string range, int min, int max, string name,
            bool monthNames, bool dayNames)
        {
            var bounds = range.Split('-');
            if (bounds.Length != 2 || bounds[0].Length == 0 || bounds[1].Length == 0)
                throw Invalid(name, range);
            var start = ParseValue(bounds[0], min, max, name, monthNames, dayNames);
            var end = ParseValue(bounds[1], min, max, name, monthNames, dayNames);
            if (start > end)
                throw new ArgumentException($"Range start greater than range end in cron field '{name}': {range}");
            return (start, end);
        }

        private static int ParseValue(string value, int min, int max, string name, bool monthNames, bool dayNames)
        {
            if (monthNames)
            {
                var month = Array.IndexOf(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames
                    .Take(12).Select(x => x.ToUpperInvariant()).ToArray(), value.ToUpperInvariant());
                if (month >= 0) return month + 1;
            }
            if (dayNames)
            {
                var day = Array.IndexOf(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames
                    .Select(x => x.ToUpperInvariant()).ToArray(), value.ToUpperInvariant());
                if (day >= 0) return day;
            }
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                throw Invalid(name, value);
            if (parsed < min || parsed > max)
                throw new ArgumentException($"Value {parsed} out of range in cron field '{name}'. Expected {min}-{max}.");
            return parsed;
        }

        private static ArgumentException Invalid(string field, string value) =>
            new ArgumentException($"Invalid value in cron field '{field}': '{value}'.");

        private static SortedSet<int> ParseDayOfMonth(string field, CronExpression result)
        {
            if (field == "L")
            {
                result._dayOfMonthSpecialKind = DayOfMonthSpecialKind.LastDayOffset;
                return new SortedSet<int>();
            }
            if (field.StartsWith("L-", StringComparison.Ordinal))
            {
                if (!int.TryParse(field.Substring(2), NumberStyles.None, CultureInfo.InvariantCulture, out var offset) || offset > 30)
                    throw Invalid("DayOfMonth", field);
                result._dayOfMonthSpecialKind = DayOfMonthSpecialKind.LastDayOffset;
                result._dayOfMonthSpecialValue = offset;
                return new SortedSet<int>();
            }
            if (field.EndsWith("W", StringComparison.Ordinal))
            {
                if (!int.TryParse(field.Substring(0, field.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var day) || day < 1 || day > 31)
                    throw Invalid("DayOfMonth", field);
                result._dayOfMonthSpecialKind = DayOfMonthSpecialKind.NearestWeekday;
                result._dayOfMonthSpecialValue = day;
                return new SortedSet<int>();
            }
            if (field.Contains("L") || field.Contains("W")) throw Invalid("DayOfMonth", field);
            return ParseField(field, 1, 31, "DayOfMonth");
        }

        private static SortedSet<int> ParseDayOfWeek(string field, CronExpression result)
        {
            if (field == "L")
            {
                result._dayOfWeekSpecialKind = DayOfWeekSpecialKind.LastSaturday;
                return new SortedSet<int>();
            }
            if (field.EndsWith("L", StringComparison.Ordinal))
            {
                var dayText = field.Substring(0, field.Length - 1);
                if (!int.TryParse(dayText, NumberStyles.None, CultureInfo.InvariantCulture, out var day) || day < 0 || day > 6)
                    throw Invalid("DayOfWeek", field);
                result._dayOfWeekSpecialKind = DayOfWeekSpecialKind.LastOccurrence;
                result._dayOfWeekSpecialValue = day;
                return new SortedSet<int>();
            }
            if (field.Contains("L") || field.Contains("#"))
            {
                if (field.Contains("#"))
                    throw new NotImplementedException("The '#' operator is not implemented yet.");
                throw Invalid("DayOfWeek", field);
            }
            return ParseField(field, 0, 6, "DayOfWeek", dayNames: true);
        }

        public DateTime? GetNextOccurrence(DateTime baseTime)
        {
            DateTime first;
            try
            {
                if (Seconds.Count == 1 && Seconds.Contains(0))
                {
                    var minute = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, baseTime.Minute, 0, baseTime.Kind);
                    first = minute.AddMinutes(1);
                }
                else
                {
                    var second = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, baseTime.Minute, baseTime.Second, baseTime.Kind);
                    first = second.AddSeconds(1);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            var date = first.Date;
            // Gregorian calendar patterns repeat every 400 years. Cap the search there,
            // while also respecting DateTime's maximum representable value.
            var lastYear = Math.Min(DateTime.MaxValue.Year, first.Year > DateTime.MaxValue.Year - 400
                ? DateTime.MaxValue.Year : first.Year + 400);
            while (date.Year <= lastYear)
            {
                if (MatchesDate(date))
                {
                    foreach (var hour in Hours)
                    {
                        foreach (var minute in Minutes)
                        {
                            foreach (var second in Seconds)
                            {
                                var candidate = new DateTime(date.Year, date.Month, date.Day, hour, minute, second, first.Kind);
                                if (candidate >= first)
                                    return candidate;
                            }
                        }
                    }
                }

                if (date.Year == DateTime.MaxValue.Year && date.Month == 12 && date.Day == 31)
                    break;
                date = date.AddDays(1);
            }
            return null;
        }

        private bool MatchesDate(DateTime date)
        {
            if (!Month.Contains(date.Month)) return false;

            bool matchesDayOfMonth;
            switch (_dayOfMonthSpecialKind)
            {
                case DayOfMonthSpecialKind.LastDayOffset:
                    matchesDayOfMonth = date.Day == DateTime.DaysInMonth(date.Year, date.Month) - _dayOfMonthSpecialValue;
                    break;
                case DayOfMonthSpecialKind.NearestWeekday:
                    matchesDayOfMonth = NearestWeekday(date.Year, date.Month, _dayOfMonthSpecialValue) == date.Day;
                    break;
                default:
                    matchesDayOfMonth = DayOfMonth.Contains(date.Day);
                    break;
            }
            if (!matchesDayOfMonth) return false;

            switch (_dayOfWeekSpecialKind)
            {
                case DayOfWeekSpecialKind.LastSaturday:
                    return date.DayOfWeek == System.DayOfWeek.Saturday;
                case DayOfWeekSpecialKind.LastOccurrence:
                    return (int)date.DayOfWeek == _dayOfWeekSpecialValue &&
                           date.Day + 7 > DateTime.DaysInMonth(date.Year, date.Month);
                default:
                    return DayOfWeek.Contains((int)date.DayOfWeek);
            }
        }

        private static int NearestWeekday(int year, int month, int requestedDay)
        {
            var lastDay = DateTime.DaysInMonth(year, month);
            if (requestedDay > lastDay) return -1;
            var day = new DateTime(year, month, requestedDay).DayOfWeek;
            if (day == System.DayOfWeek.Saturday) return requestedDay == 1 ? 3 : requestedDay - 1;
            if (day == System.DayOfWeek.Sunday) return requestedDay == lastDay ? requestedDay - 2 : requestedDay + 1;
            return requestedDay;
        }
    }
}
