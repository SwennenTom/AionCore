using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Infrastructure.Interfaces;

namespace AionCoreBot.Application.Maintenance
{
    public class DataCleanupService : IDataCleanupService
    {
        private readonly ICandleRepository _candleRepository;
        private readonly ISignalEvaluationRepository _signalRepo;

        public DataCleanupService(ICandleRepository candleRepository, ISignalEvaluationRepository signalRepo)
        {
            _candleRepository = candleRepository;
            _signalRepo = signalRepo;
        }

        public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
        {
            await _candleRepository.ClearAllAsync();
            await _signalRepo.ClearAllAsync();
        }
    }
}
