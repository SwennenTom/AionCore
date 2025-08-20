using AionCoreBot.Application.Maintenance;
using AionCoreBot.Application.Logging;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Interfaces;

public class DataCleanupService : IDataCleanupService
{
    private readonly ICandleRepository _candleRepository;
    private readonly ISignalEvaluationRepository _signalRepo;
    private readonly IAccountBalanceRepository _accountBalanceRepository;
    private readonly IIndicatorRepository<EMAResult> _emaRepository;
    private readonly IIndicatorRepository<ATRResult> _atrRepository;
    private readonly IIndicatorRepository<RSIResult> _rsiRepository;
    private readonly ILogService _logService;

    public DataCleanupService(
        ICandleRepository candleRepository,
        ISignalEvaluationRepository signalRepo,
        IAccountBalanceRepository accountBalanceRepository,
        IIndicatorRepository<EMAResult> emaRepository,
        IIndicatorRepository<ATRResult> atrRepository,
        IIndicatorRepository<RSIResult> rsiRepository,
        ILogService logService)
    {
        _candleRepository = candleRepository;
        _signalRepo = signalRepo;
        _accountBalanceRepository = accountBalanceRepository;
        _emaRepository = emaRepository;
        _atrRepository = atrRepository;
        _rsiRepository = rsiRepository;
        _logService = logService;
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _candleRepository.ClearAllAsync();
        await _signalRepo.ClearAllAsync();
        await _accountBalanceRepository.ClearAllAsync();

        await _emaRepository.ClearAllAsync();
        await _atrRepository.ClearAllAsync();
        await _rsiRepository.ClearAllAsync();
    }

    public async Task ClearOldLogging(CancellationToken cancellationToken = default)
    {
        await _logService.DeleteOldLogsAsync(cancellationToken);
    }
}

