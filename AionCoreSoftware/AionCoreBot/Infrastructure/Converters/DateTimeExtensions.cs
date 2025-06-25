using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Converters
{
    public static class DateTimeExtensions
    {
        public static DateTime RoundUpToNextMinute(this DateTime dt)
        {
            if (dt.Second > 0 || dt.Millisecond > 0)
            {
                dt = dt.AddSeconds(60 - dt.Second).AddMilliseconds(-dt.Millisecond);
            }
            return dt;
        }
    }

}
