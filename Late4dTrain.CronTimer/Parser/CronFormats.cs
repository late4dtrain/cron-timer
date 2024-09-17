using System;

namespace Late4dTrain.CronTimer.Parser
{
    [Flags]
    public enum CronFormats
    {
        None = 0,
        Standard = 1,
        IncludeSeconds = 2,
    }

    public static class CronExpressionTypeExtensions
    {
        public static bool HasFlagFast(this CronFormats value, CronFormats flag)
        {
            return (value & flag) != 0;
        }
    }
}
