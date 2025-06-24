using AionCoreBot.Application.Interfaces.IAnalyzers;
using AionCoreBot.Domain.Models;
using AionCoreBot.Worker.Interfaces;

namespace AionCoreBot.Worker.Services
{
    public class AnalyzerWorker : IAnalyzerWorker
    {
        private readonly IIndicatorService<EMAResult> _emaService;
        private readonly IIndicatorService<ATRResult> _atrService;
        private readonly IIndicatorService<RSIResult> _rsiService;

        public AnalyzerWorker(
            IIndicatorService<EMAResult> emaService,
            IIndicatorService<ATRResult> atrService,
            IIndicatorService<RSIResult> rsiService)
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
