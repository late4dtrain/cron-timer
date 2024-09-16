using System;
using System.Collections.Generic;
using System.Linq;

namespace Late4dTrain.CronTimer
{
    public class CronExpression
    {
        public List<int> Seconds { get; set; }
        public List<int> Minutes { get; set; }
        public List<int> Hours { get; set; }
        public List<int> DayOfMonth { get; set; }
        public List<int> Month { get; set; }
        public List<int> DayOfWeek { get; set; }

        public static CronExpression Parse(string expression, CronExpressionType expressionType)
        {
            string[] parts = expression.Split(' ');

            int expectedFieldCount = expressionType == CronExpressionType.WithSeconds ? 6 : 5;

            if (parts.Length != expectedFieldCount)
                throw new ArgumentException($"Invalid cron expression: {expression}");

            CronExpression cron = new CronExpression();

            int index = 0;

            if (expressionType == CronExpressionType.WithSeconds)
            {
                cron.Seconds = ParseField(parts[index++], 0, 59);
            }
            else
            {
                cron.Seconds = new List<int> { 0 }; // default to 0 seconds
            }

            cron.Minutes = ParseField(parts[index++], 0, 59);
            cron.Hours = ParseField(parts[index++], 0, 23);
            cron.DayOfMonth = ParseField(parts[index++], 1, 31);
            cron.Month = ParseField(parts[index++], 1, 12);
            cron.DayOfWeek = ParseField(parts[index++], 0, 6); // Sunday = 0

            return cron;
        }

        private static List<int> ParseField(string field, int minValue, int maxValue)
        {
            List<int> values = new List<int>();

            string[] parts = field.Split(',');

            foreach (string part in parts)
            {
                if (part.Contains("/"))
                {
                    // Step values
                    string[] stepParts = part.Split('/');
                    string rangePart = stepParts[0];
                    int step = int.Parse(stepParts[1]);

                    int rangeStart = minValue;
                    int rangeEnd = maxValue;

                    if (!string.IsNullOrEmpty(rangePart) && rangePart != "*")
                    {
                        if (rangePart.Contains("-"))
                        {
                            string[] rangeBounds = rangePart.Split('-');
                            rangeStart = int.Parse(rangeBounds[0]);
                            rangeEnd = int.Parse(rangeBounds[1]);
                        }
                        else
                        {
                            rangeStart = int.Parse(rangePart);
                            rangeEnd = int.Parse(rangePart);
                        }
                    }

                    for (int i = rangeStart; i <= rangeEnd; i++)
                    {
                        if ((i - rangeStart) % step == 0)
                        {
                            values.Add(i);
                        }
                    }
                }
                else
                {
                    // Range or single value
                    List<int> rangeValues = ParseRange(part, minValue, maxValue);
                    values.AddRange(rangeValues);
                }
            }

            // Remove duplicates and sort
            values = values.Distinct().OrderBy(x => x).ToList();

            return values;
        }

        private static List<int> ParseRange(string rangePart, int minValue, int maxValue)
        {
            List<int> values = new List<int>();

            if (rangePart == "*")
            {
                for (int i = minValue; i <= maxValue; i++)
                    values.Add(i);
            }
            else if (rangePart.Contains("-"))
            {
                string[] rangeBounds = rangePart.Split('-');
                int start = int.Parse(rangeBounds[0]);
                int end = int.Parse(rangeBounds[1]);

                for (int i = start; i <= end; i++)
                {
                    values.Add(i);
                }
            }
            else
            {
                // Single value
                int val = int.Parse(rangePart);
                values.Add(val);
            }

            return values;
        }

        public DateTime? GetNextOccurrence(DateTime baseTime)
        {
            DateTime next = baseTime;

            bool hasSeconds = Seconds != null && Seconds.Count > 0 && !(Seconds.Count == 1 && Seconds[0] == 0);

            if (hasSeconds)
            {
                next = next.AddSeconds(1);
            }
            else
            {
                next = next.AddMinutes(1);
                next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, next.Kind);
            }

            while (true)
            {
                if (Month.Contains(next.Month))
                {
                    if (DayOfMonth.Contains(next.Day))
                    {
                        if (DayOfWeek.Contains((int)next.DayOfWeek))
                        {
                            if (Hours.Contains(next.Hour))
                            {
                                if (Minutes.Contains(next.Minute))
                                {
                                    if (hasSeconds)
                                    {
                                        if (Seconds.Contains(next.Second))
                                        {
                                            // Found matching time
                                            return next;
                                        }
                                    }
                                    else
                                    {
                                        // Found matching time
                                        return next;
                                    }
                                }
                            }
                        }
                    }
                }

                // Increment time
                if (hasSeconds)
                {
                    next = next.AddSeconds(1);
                }
                else
                {
                    next = next.AddMinutes(1);
                }

                // Prevent infinite loops
                if ((next - baseTime).TotalDays > 366)
                {
                    // No occurrence found within a year
                    return null;
                }
            }
        }
    }
}
