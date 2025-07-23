using AionCoreBot.Application.Maintenance;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Infrastructure.Interfaces;

public class DataCleanupService : IDataCleanupService
{
    private readonly IEnumerable<IIndicatorRepository<IIndicatorResult>> _indicatorRepositories;
    private readonly ICandleRepository _candleRepository;
    private readonly ISignalEvaluationRepository _signalRepo;
    private readonly IAccountBalanceRepository _accountBalanceRepository;

    public DataCleanupService(
        ICandleRepository candleRepository,
        ISignalEvaluationRepository signalRepo,
        IAccountBalanceRepository accountBalanceRepository,
        IEnumerable<IIndicatorRepository<IIndicatorResult>> indicatorRepositories)
    {
        _candleRepository = candleRepository;
        _signalRepo = signalRepo;
        _accountBalanceRepository = accountBalanceRepository;
        _indicatorRepositories = indicatorRepositories;
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _candleRepository.ClearAllAsync();
        await _signalRepo.ClearAllAsync();
        await _accountBalanceRepository.ClearAllAsync();

        foreach (var repo in _indicatorRepositories)
        {
            await repo.ClearAllAsync();
        }
    }
}
