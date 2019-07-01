using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Late4Train.CronTimer.Extensions
{
    public static class DateTimeExtensions
    {
        internal static DateTime ToFlat(this DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc) throw new ArgumentException("The time is not UTC.");
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second,
                DateTimeKind.Utc);
        } 
    }
}
