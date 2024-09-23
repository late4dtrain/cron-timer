using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Late4dTrain.CronTimer.Parser
{
    public class CronTab
    {
        public CronTab(string expression, CronFormats formats)
        {
            (Expression, Formats, Id) = (expression, formats, Guid.NewGuid());
            ValidateExpression();
        }

        public Guid Id { get; set; }
        public string Expression { get; set; }
        public CronFormats Formats { get; set; }

        private void ValidateExpression()
        {
            string[] fields = Expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int expectedFieldCount = Formats.HasFlagFast(CronFormats.IncludeSeconds) ? 6 : 5;

            if (fields.Length != expectedFieldCount)
            {
                throw new ArgumentException(
                    $"Invalid number of fields in cron expression. Expected {expectedFieldCount}, got {fields.Length}.");
            }

            int fieldIndex = 0;

            try
            {
                if (Formats.HasFlagFast(CronFormats.IncludeSeconds))
                {
                    ValidateField(fields[fieldIndex++], 0, 59, "seconds");
                }

                ValidateField(fields[fieldIndex++], 0, 59, "minutes");
                ValidateField(fields[fieldIndex++], 0, 23, "hours");
                ValidateDayOfMonthField(fields[fieldIndex++]);
                ValidateMonthField(fields[fieldIndex++]);
                ValidateDayOfWeekField(fields[fieldIndex++]);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid cron expression.", ex);
            }
        }

        private void ValidateField(string field, int minValue, int maxValue, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException($"The {fieldName} field is empty.");
            }

            // Allowed patterns: "*", "*/5", "1-10", "1,2,3", "5"
            string pattern = @"^(\*(\/\d+)?|\d+(-\d+)?(\/\d+)?)(,\d+(-\d+)?(\/\d+)?)*$";

            if (!Regex.IsMatch(field, pattern))
            {
                throw new ArgumentException($"Invalid {fieldName} field format.");
            }

            // Split by commas to handle lists
            var parts = field.Split(',');

            foreach (var part in parts)
            {
                if (part == "*")
                {
                    continue;
                }

                if (part.Contains("/"))
                {
                    var stepParts = part.Split('/');
                    if (stepParts.Length != 2)
                        throw new ArgumentException($"Invalid step value in {fieldName} field.");

                    ValidateRangeOrValue(stepParts[0], minValue, maxValue, fieldName);
                    int step = int.Parse(stepParts[1]);
                    if (step <= 0)
                        throw new ArgumentException(
                            $"Invalid step value in {fieldName} field. Step must be greater than zero.");
                }
                else
                {
                    ValidateRangeOrValue(part, minValue, maxValue, fieldName);
                }
            }
        }

        private void ValidateRangeOrValue(string part, int minValue, int maxValue, string fieldName)
        {
            if (part.Contains("-"))
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length != 2)
                    throw new ArgumentException($"Invalid range in {fieldName} field.");

                int start = int.Parse(rangeParts[0]);
                int end = int.Parse(rangeParts[1]);

                if (start < minValue || start > maxValue || end < minValue || end > maxValue || start > end)
                    throw new ArgumentException(
                        $"Invalid range in {fieldName} field. Range values must be between {minValue} and {maxValue}.");
            }
            else if (part != "*")
            {
                int value = int.Parse(part);
                if (value < minValue || value > maxValue)
                    throw new ArgumentException(
                        $"Invalid value '{value}' in {fieldName} field. Value must be between {minValue} and {maxValue}.");
            }
        }

        private void ValidateDayOfMonthField(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("The day of month field is empty.");
            }

            // Allowed patterns: "*", "?", "L", "LW", "1W", "15", "1-15", "*/5", "1,15,30"
            string pattern =
                @"^(\?|L|LW|\d{1,2}W?|\*(\/\d+)?|\d{1,2}(-\d{1,2})?(\/\d+)?)(,\d{1,2}(-\d{1,2})?(\/\d+)?)*$";

            if (!Regex.IsMatch(field, pattern))
            {
                throw new ArgumentException("Invalid day of month field format.");
            }

            // Additional validation
            if (field == "L" || field == "LW" || field == "?" || field == "*")
            {
                return; // Valid special values
            }
            else if (field.Contains("W"))
            {
                var wParts = field.Split('W');
                if (wParts.Length != 2 || !int.TryParse(wParts[0], out int day) || day < 1 || day > 31)
                {
                    throw new ArgumentException("Invalid 'W' usage in day of month field.");
                }
            }
            else
            {
                ValidateField(field, 1, 31, "day of month");
            }
        }

        private void ValidateMonthField(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("The month field is empty.");
            }

            string[] validMonths =
                { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

            // Allowed patterns: "*", "JAN", "FEB", "1-12", "*/2", "JAN-MAR", "1,6,12"
            string pattern =
                @"^(\*|\d{1,2}|\d{1,2}-\d{1,2}|\d{1,2}/\d+|[A-Z]{3}(-[A-Z]{3})?)(,\d{1,2}|\*[\/\d]*|[A-Z]{3}(-[A-Z]{3})?)*$";

            if (!Regex.IsMatch(field, pattern))
            {
                throw new ArgumentException("Invalid month field format.");
            }

            // Split by commas to handle lists
            var parts = field.Split(',');

            foreach (var part in parts)
            {
                if (part == "*")
                {
                    continue;
                }

                if (part.Contains("-"))
                {
                    var rangeParts = part.Split('-');
                    ValidateMonthValue(rangeParts[0]);
                    ValidateMonthValue(rangeParts[1]);
                }
                else if (part.Contains("/"))
                {
                    var stepParts = part.Split('/');
                    ValidateMonthValue(stepParts[0]);
                    int step = int.Parse(stepParts[1]);
                    if (step <= 0)
                        throw new ArgumentException(
                            "Invalid step value in month field. Step must be greater than zero.");
                }
                else
                {
                    ValidateMonthValue(part);
                }
            }
        }

        private void ValidateMonthValue(string value)
        {
            string[] validMonths =
                { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

            if (int.TryParse(value, out int month))
            {
                if (month < 1 || month > 12)
                    throw new ArgumentException($"Invalid month value '{month}'. Month must be between 1 and 12.");
            }
            else if (validMonths.Contains(value.ToUpper()))
            {
                // Valid month abbreviation
            }
            else
            {
                throw new ArgumentException($"Invalid month value '{value}'.");
            }
        }

        private void ValidateDayOfWeekField(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("The day of week field is empty.");
            }

            // Allowed patterns: "*", "?", "L", "1-7", "SUN-SAT", "*/1", "MON-FRI", "5L", "5#3"
            string pattern =
                @"^(\*|\?|L|\d{1}(-\d{1})?|\d{1}L|\d{1}#\d{1}|[A-Z]{3}(-[A-Z]{3})?)(,\d{1}|,?[A-Z]{3}(-[A-Z]{3})?)*$";

            if (!Regex.IsMatch(field, pattern))
            {
                throw new ArgumentException("Invalid day of week field format.");
            }

            // Split the field by commas to handle lists
            var parts = field.Split(',');

            foreach (var part in parts)
            {
                if (part.Contains("#"))
                {
                    var hashParts = part.Split('#');
                    if (hashParts.Length != 2 || !int.TryParse(hashParts[1], out int nth) || nth < 1 || nth > 5)
                    {
                        throw new ArgumentException("Invalid '#' usage in day of week field.");
                    }

                    ValidateDayOfWeekValue(hashParts[0]);
                }
                else if (part.EndsWith("L"))
                {
                    ValidateDayOfWeekValue(part.Substring(0, part.Length - 1));
                }
                else if (part == "*" || part == "?" || part == "L")
                {
                    continue; // Valid special values
                }
                else if (part.Contains("-"))
                {
                    var rangeParts = part.Split('-');
                    if (rangeParts.Length != 2)
                    {
                        throw new ArgumentException($"Invalid range in day of week field: '{part}'.");
                    }

                    ValidateDayOfWeekValue(rangeParts[0]);
                    ValidateDayOfWeekValue(rangeParts[1]);
                }
                else
                {
                    ValidateDayOfWeekValue(part);
                }
            }
        }

        private void ValidateDayOfWeekValue(string value)
        {
            string[] validDays = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

            if (int.TryParse(value, out int dayOfWeek))
            {
                if (dayOfWeek < 0 || dayOfWeek > 7)
                    throw new ArgumentException(
                        $"Invalid day of week value '{dayOfWeek}'. Day of week must be between 0 and 7.");
            }
            else if (validDays.Contains(value.ToUpper()))
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
