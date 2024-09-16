using System;

namespace Late4dTrain.CronTimer
{
    [Flags]
    public enum CronExpressionType
    {
        None = 0,
        Standard = 1,
        IncludeSeconds = 2,
    }

    public static class CronExpressionTypeExtensions
    {
        public static bool HasFlagFast(this CronExpressionType value, CronExpressionType flag)
        {
            return (value & flag) != 0;
        }
    }
}
