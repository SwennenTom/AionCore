using AionCoreBot.Application.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace AionCoreBot.Application.Analyzers
{
    public class RSIAnalyzer : IAnalyzer
    {
        private readonly IIndicatorService<RSIResult> _rsiService;
        private readonly IConfiguration _configuration;

        public RSIAnalyzer(IIndicatorService<RSIResult> rsiService, IConfiguration configuration)
        {
            _rsiService = rsiService;
            _configuration = configuration;
        }

        public async Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval)
        {
            int period = _configuration.GetValue<int>("IndicatorParameters:RSI:Period", 14);
            int overbought = _configuration.GetValue<int>("IndicatorParameters:RSI:OverboughtThreshold", 70);
            int oversold = _configuration.GetValue<int>("IndicatorParameters:RSI:OversoldThreshold", 30);

            var rsi = await _rsiService.GetLatestAsync(symbol, interval, period);

            var result = new SignalEvaluationResult
            {
                Symbol = symbol,
                Interval = interval,
                EvaluationTime = DateTime.UtcNow,
                IndicatorValues = new Dictionary<string, decimal>(),
                SignalDescriptions = new List<string>(),
                ProposedAction = TradeAction.Hold
            };

            if (rsi != null)
            {
                result.IndicatorValues[$"RSI{period}"] = rsi.Value;

                if (rsi.Value < oversold)
                {
                    result.ProposedAction = TradeAction.Buy;
                    result.SignalDescriptions.Add("RSI oversold");
                    result.Reason = $"RSI under {oversold}";
                }
                else if (rsi.Value > overbought)
                {
                    result.ProposedAction = TradeAction.Sell;
                    result.SignalDescriptions.Add("RSI overbought");
                    result.Reason = $"RSI over {overbought}";
                }
            }

            return result;
        }
    }
}