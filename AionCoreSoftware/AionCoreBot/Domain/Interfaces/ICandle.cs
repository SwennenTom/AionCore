using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Interfaces
{
    public interface ICandle
    {
        String Symbol { get;}
        DateTime OpenTime { get; }
        DateTime CloseTime { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal HighPrice { get; }
        decimal LowPrice { get; }
        decimal Volume { get; }
        decimal BodySize => Math.Abs(ClosePrice - OpenPrice);
        decimal TotalRange => HighPrice - LowPrice;
        bool IsBullish => ClosePrice > OpenPrice;
        bool IsBearish => ClosePrice < OpenPrice;
        decimal UpperWick => HighPrice - Math.Max(OpenPrice, ClosePrice);
        decimal LowerWick => Math.Min(OpenPrice, ClosePrice) - LowPrice;
    }
}
