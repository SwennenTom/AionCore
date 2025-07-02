using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Helpers
{
    public static class DateTimeExtensions
    {        public static DateTime RoundUpToNextHour(this DateTime dt)
        {
            return dt.AddHours(1).Date.AddHours(dt.AddHours(1).Hour);
        }
    }
}
