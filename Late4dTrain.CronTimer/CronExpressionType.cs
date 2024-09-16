using System;

namespace Late4dTrain.CronTimer
{
    [Flags]
    public enum CronExpressionType
    {
        None = 0,

        WithSeconds = 1
    }

    public static class CronExpressionTypeExtensions
    {
        public static bool HasFlagFast(this CronExpressionType value, CronExpressionType flag)
        {
            return (value & flag) != 0;
        }
    }
}
