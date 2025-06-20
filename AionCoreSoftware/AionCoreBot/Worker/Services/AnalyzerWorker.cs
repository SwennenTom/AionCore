using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Worker.Indicators;
using AionCoreBot.Worker.Interfaces;

namespace AionCoreBot.Worker.Services
{
    public class AnalyzerWorker : IAnalyzerWorker
    {
        private readonly IEMAService _emaService;
        private readonly IATRService _atrService;
        private readonly IRSIService _rsiService;

        public AnalyzerWorker(IEMAService emaService, IATRService atrService, IRSIService rsiService)
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
