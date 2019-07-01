using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Late4Train.CronTimer.Extensions
{
    internal static class LongExtensions
    {
        internal static int ToMilliseconds(this long value)
        {
            return (int) (value / 10000);
        } 
    }
}
