using AionCoreBot.Helpers.Converters;
using System;
using System.Text.Json.Serialization;

namespace AionCoreBot.Domain.Models
{
    public class BinanceKlineMessage
    {
        [JsonPropertyName("stream")]
        public string Stream { get; set; }

        [JsonPropertyName("data")]
        public KlineWrapper Data { get; set; }

        public class KlineWrapper
        {
            [JsonPropertyName("e")]
            public string EventType { get; set; }

            [JsonPropertyName("E")]
            public long EventTime { get; set; }

            [JsonPropertyName("s")]
            public string Symbol { get; set; }

            [JsonPropertyName("k")]
            public KlineData Kline { get; set; }
        }

        public class KlineData
        {
            [JsonPropertyName("t")]
            public long StartTime { get; set; }

            [JsonPropertyName("T")]
            public long CloseTime { get; set; }

            [JsonPropertyName("s")]
            public string Symbol { get; set; }

            [JsonPropertyName("i")]
            public string Interval { get; set; }

            [JsonPropertyName("f")]
            public long FirstTradeId { get; set; }

            [JsonPropertyName("L")]
            public long LastTradeId { get; set; }

            [JsonPropertyName("o")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal OpenPrice { get; set; }

            [JsonPropertyName("c")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal ClosePrice { get; set; }

            [JsonPropertyName("h")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal HighPrice { get; set; }

            [JsonPropertyName("l")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal LowPrice { get; set; }

            [JsonPropertyName("v")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal Volume { get; set; }

            [JsonPropertyName("n")]
            public int NumberOfTrades { get; set; }

            [JsonPropertyName("x")]
            public bool IsFinal { get; set; }

            [JsonPropertyName("q")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal QuoteAssetVolume { get; set; }

            [JsonPropertyName("V")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal TakerBuyBaseAssetVolume { get; set; }

            [JsonPropertyName("Q")]
            [JsonConverter(typeof(StringToDecimalConverter))]
            public decimal TakerBuyQuoteAssetVolume { get; set; }

            [JsonPropertyName("B")]
            public string Ignore { get; set; }
        }
    }

}
