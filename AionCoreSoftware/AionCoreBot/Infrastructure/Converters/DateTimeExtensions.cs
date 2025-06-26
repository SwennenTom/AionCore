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

        public static DateTime AlignToInterval(this DateTime dt, string interval)
        {
            dt = dt.ToUniversalTime();

            return interval switch
            {
                "1m" => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc),
                "5m" => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, (dt.Minute / 5) * 5, 0, DateTimeKind.Utc),
                "15m" => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, (dt.Minute / 15) * 15, 0, DateTimeKind.Utc),
                "1h" => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc),
                "4h" => new DateTime(dt.Year, dt.Month, dt.Day, (dt.Hour / 4) * 4, 0, 0, DateTimeKind.Utc),
                "1d" => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc),
                _ => throw new ArgumentException($"Unknown interval: {interval}")
            };
        }
    }
}
