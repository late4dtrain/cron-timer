using System;
using System.Collections.Generic;
using System.Linq;

namespace Late4dTrain.CronTimer
{
    public class CronExpression
    {
        public SortedSet<int> Seconds { get; set; }
        public SortedSet<int> Minutes { get; set; }
        public SortedSet<int> Hours { get; set; }
        public SortedSet<int> DayOfMonth { get; set; }
        public SortedSet<int> Month { get; set; }
        public SortedSet<int> DayOfWeek { get; set; }

        public static CronExpression Parse(string expression, CronExpressionType expressionType)
        {
            string[] parts = expression.Split(' ');

            int expectedFieldCount = expressionType == CronExpressionType.WithSeconds ? 6 : 5;

            if (parts.Length != expectedFieldCount)
                throw new ArgumentException($"Invalid cron expression: {expression}");

            var cron = new CronExpression();

            int index = 0;

            if (expressionType == CronExpressionType.WithSeconds)
            {
                cron.Seconds = ParseField(parts[index++], 0, 59);
            }
            else
            {
                cron.Seconds = new SortedSet<int> { 0 }; // default to 0 seconds
            }

            cron.Minutes = ParseField(parts[index++], 0, 59);
            cron.Hours = ParseField(parts[index++], 0, 23);
            cron.DayOfMonth = ParseField(parts[index++], 1, 31);
            cron.Month = ParseField(parts[index++], 1, 12);
            cron.DayOfWeek = ParseField(parts[index++], 0, 6); // Sunday = 0

            return cron;
        }

        private static SortedSet<int> ParseField(string field, int minValue, int maxValue)
        {
            var values = new SortedSet<int>();

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
                    // Handle step values
                    var stepParts = part.Split('/');
                    var rangePart = stepParts[0];
                    if (!int.TryParse(stepParts[1], out int step) || step <= 0)
                        throw new ArgumentException($"Invalid step value in cron field: {part}");

                    int rangeStart = minValue;
                    int rangeEnd = maxValue;

                    if (!string.IsNullOrEmpty(rangePart) && rangePart != "*")
                    {
                        if (rangePart.Contains("-"))
                        {
                            var rangeBounds = rangePart.Split('-');
                            if (!int.TryParse(rangeBounds[0], out rangeStart) ||
                                !int.TryParse(rangeBounds[1], out rangeEnd))
                                throw new ArgumentException($"Invalid range in cron field: {part}");
                        }
                        else
                        {
                            if (!int.TryParse(rangePart, out rangeStart))
                                throw new ArgumentException($"Invalid value in cron field: {part}");
                            rangeEnd = rangeStart;
                        }
                    }

                    for (int i = rangeStart; i <= rangeEnd; i += step)
                    {
                        if (i >= minValue && i <= maxValue)
                            values.Add(i);
                    }
                }
                else if (part.Contains("-"))
                {
                    // Handle ranges
                    var rangeBounds = part.Split('-');
                    if (!int.TryParse(rangeBounds[0], out int start) || !int.TryParse(rangeBounds[1], out int end))
                        throw new ArgumentException($"Invalid range in cron field: {part}");

                    for (int i = start; i <= end; i++)
                    {
                        if (i >= minValue && i <= maxValue)
                            values.Add(i);
                    }
                }
                else
                {
                    // Single value
                    if (int.TryParse(part, out int val))
                    {
                        if (val < minValue || val > maxValue)
                            throw new ArgumentException($"Value {val} out of range in cron field: {field}");
                        values.Add(val);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid value in cron field: {part}");
                    }
                }
            }

            return values;
        }

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
                    int nextMonth = GetNextValue(Month, next.Month, 12);
                    int yearsToAdd = nextMonth <= next.Month ? 1 : 0;
                    next = new DateTime(next.Year + yearsToAdd, nextMonth, 1, 0, 0, 0, next.Kind);
                    continue;
                }

                if (!DayOfMonth.Contains(next.Day))
                {
                    // Move to the next valid day
                    int nextDay = GetNextValue(DayOfMonth, next.Day, DateTime.DaysInMonth(next.Year, next.Month));
                    if (nextDay <= next.Day)
                    {
                        next = next.AddMonths(1);
                        next = new DateTime(next.Year, next.Month, 1, 0, 0, 0, next.Kind);
                        continue;
                    }

                    next = new DateTime(next.Year, next.Month, nextDay, next.Hour, next.Minute, next.Second, next.Kind);
                    continue;
                }

                if (!DayOfWeek.Contains((int)next.DayOfWeek))
                {
                    // Move to the next valid day
                    next = next.AddDays(1);
                    next = new DateTime(next.Year, next.Month, next.Day, 0, 0, 0, next.Kind);
                    continue;
                }

                if (!Hours.Contains(next.Hour))
                {
                    // Move to the next valid hour
                    int nextHour = GetNextValue(Hours, next.Hour, 24);
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
                    int nextMinute = GetNextValue(Minutes, next.Minute, 60);
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
                        int nextSecond = GetNextValue(Seconds, next.Second, 60);
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

        private int GetNextValue(SortedSet<int> values, int currentValue, int maxValue)
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
