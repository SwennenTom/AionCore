using AionCoreBot.Application.Interfaces.IIndicators;
using AionCoreBot.Domain.Models;
using AionCoreBot.Worker.Interfaces;

namespace AionCoreBot.Worker.Services
{
    public class AnalyzerWorker : IAnalyzerWorker
    {
        private readonly IBaseIndicatorService<EMAResult> _emaService;
        private readonly IBaseIndicatorService<ATRResult> _atrService;
        private readonly IBaseIndicatorService<RSIResult> _rsiService;

        public AnalyzerWorker(
            IBaseIndicatorService<EMAResult> emaService,
            IBaseIndicatorService<ATRResult> atrService,
            IBaseIndicatorService<RSIResult> rsiService)
        {
            _emaService = emaService;
            _atrService = atrService;
            _rsiService = rsiService;
        }

        public async Task RunAllAsync()
        {
            Console.WriteLine("[ANALYZERS] Running EMA, ATR, and RSI analyzers...");

            var emaTask = _emaService.CalcAllAsync();
            var atrTask = _atrService.CalcAllAsync();
            var rsiTask = _rsiService.CalcAllAsync();

            await Task.WhenAll(emaTask, atrTask, rsiTask);

            Console.WriteLine("[ANALYZERS] All analyzers completed.");
        }
    }
}
