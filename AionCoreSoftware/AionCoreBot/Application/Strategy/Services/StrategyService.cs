using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Strategy.Services
{
    public class StrategyService : IStrategyService
    {
        private readonly ISignalEvaluationRepository _signalRepository;
        private readonly IStrategizer _strategizer;
        private readonly IRiskManagementService _riskManager;
        private readonly IBalanceProvider _balanceProvider;
        private readonly ITradeManager _tradeManager;
        private readonly IConfiguration _config;

        public StrategyService(
            ISignalEvaluationRepository signalRepository,
            IStrategizer strategizer,
            IRiskManagementService riskManager,
            IBalanceProvider balanceProvider,
            ITradeManager tradeManager,
            IConfiguration config)
        {
            _signalRepository = signalRepository;
            _strategizer = strategizer;
            _riskManager = riskManager;
            _balanceProvider = balanceProvider;
            _tradeManager = tradeManager;
            _config = config;
        }

        public async Task ExecuteStrategyAsync(CancellationToken ct = default)
        {
            var latest = await _signalRepository.GetLatestSignalsAsync();
            var grouped = latest.GroupBy(s => (s.Symbol, s.Interval));

            foreach (var grp in grouped)
            {
                var decision = await _strategizer.DecideTradeAsync(grp.ToList(), ct);
                Console.WriteLine($"[STRATEGY] Beslissing {grp.Key.Symbol}: {decision.Action}");

                if (decision.Action != TradeAction.Buy) continue;

                try
                {
                    decimal lastPrice = await _balanceProvider.GetLastPriceAsync(grp.Key.Symbol, ct);
                    decimal currentPos = await _balanceProvider.GetPositionSizeAsync(grp.Key.Symbol, ct);
                    decimal maxRisk = await _riskManager.CalculateMaxRiskAmountAsync(grp.Key.Symbol, currentPos, ct);
                    decimal qty = _riskManager.CalculatePositionSize(grp.Key.Symbol, maxRisk, lastPrice);

                    if (!_riskManager.IsTradeWithinRiskLimits(grp.Key.Symbol, decision.Action, qty))
                    {
                        Console.WriteLine($"[RISK] Afgekeurd voor {grp.Key.Symbol}");
                        continue;
                    }

                    var trade = await _tradeManager.OpenTradeAsync(decision, lastPrice, qty, ct);
                    Console.WriteLine($"[ORDER] Open {trade.Symbol} qty={trade.Quantity} @ {trade.OpenPrice}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {grp.Key.Symbol}: {ex.Message}");
                }
            }
        }
    }
}
