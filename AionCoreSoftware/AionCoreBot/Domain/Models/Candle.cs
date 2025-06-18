using AionCoreBot.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class Candle : ICandle
    {        
        public string Symbol { get; set; }
        public string Interval { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get;  set; }
        public DateTime Timestamp => CloseTime;
        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal QuoteVolume { get; set; }

        //Calculated properties
        public decimal BodySize => Math.Abs(ClosePrice - OpenPrice);
        public decimal TotalRange => HighPrice - LowPrice;
        public bool IsBullish => ClosePrice > OpenPrice;
        public bool IsBearish => ClosePrice < OpenPrice;
        public decimal UpperWick => HighPrice - Math.Max(OpenPrice, ClosePrice);
        public decimal LowerWick => Math.Min(OpenPrice, ClosePrice) - LowPrice;

        public Candle() { }
        public Candle(string symbol, DateTime openTime, DateTime closeTime,
                     decimal open, decimal high, decimal low, decimal close,
                     decimal volume, decimal quoteVolume = 0)
        {
            Symbol = symbol;
            OpenTime = openTime;
            CloseTime = closeTime;
            OpenPrice = open;
            HighPrice = high;
            LowPrice = low;
            ClosePrice = close;
            Volume = volume;
            QuoteVolume = quoteVolume;
        }
    }
}
