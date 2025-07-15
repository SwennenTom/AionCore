using AionCoreBot;
using AionCoreBot.Application.Account.Interfaces;
using AionCoreBot.Application.Account.Services;
using AionCoreBot.Application.Analysis.Indicators;
using AionCoreBot.Application.Analysis.Interfaces;
using AionCoreBot.Application.Candles.Interfaces;
using AionCoreBot.Application.Candles.Services;
using AionCoreBot.Application.Maintenance;
using AionCoreBot.Application.Risk.Interfaces;
using AionCoreBot.Application.Risk.Services;
using AionCoreBot.Application.Signals.Interfaces;
using AionCoreBot.Application.Signals.Services;
using AionCoreBot.Application.Strategy.Interfaces;
using AionCoreBot.Application.Strategy.Services;
using AionCoreBot.Application.Trades.Interfaces;
using AionCoreBot.Application.Trades.Services;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Comms.Clients;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IAccountSyncService, AccountSyncService>();
builder.Services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
builder.Services.AddScoped<IBalanceHistoryRepository, BalanceHistoryRepository>();
builder.Services.AddScoped<IBalanceProvider, BalanceProvider>();

builder.Services.AddScoped<BotWorker>();
builder.Services.AddScoped<CandleAggregator>();
builder.Services.AddScoped<IAnalyzerWorker, AnalyzerWorker>();
builder.Services.AddHostedService<ScopedWorkerHostedService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategizer, Strategizer>();
builder.Services.AddScoped<ISignalInitializationService, SignalInitializationService>();
builder.Services.AddScoped<ICandleInitializationService, CandleInitializationService>();
builder.Services.AddScoped<IDataCleanupService, DataCleanupService>();
builder.Services.AddScoped<IRiskManagementService, RiskManagementService>();
builder.Services.AddScoped<ITradeManager, TradeManager>();
builder.Services.AddScoped<TradeDecisionService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DatabaseConnection")));

builder.Services.AddScoped<BinanceWebSocketService>();
builder.Services.AddScoped<ICandleDownloadService, BinanceCandleDownloadService>();
builder.Services.AddScoped<IBinanceRestClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["BinanceAccount:ApiKey"];
    var apiSecret = config["BinanceAccount:ApiSecret"];
    return new BinanceRestClient(apiKey, apiSecret);
});

builder.Services.AddScoped<ICandleRepository, CandleRepository>();
builder.Services.AddScoped<IIndicatorRepository<EMAResult>, EMARepository>();
builder.Services.AddScoped<IIndicatorRepository<RSIResult>, RSIRepository>();
builder.Services.AddScoped<IIndicatorRepository<ATRResult>, ATRRepository>();
builder.Services.AddScoped<ISignalEvaluationRepository, SignalEvaluationRepository>();

builder.Services.AddScoped<IIndicatorService<ATRResult>, ATRService>();
builder.Services.AddScoped<IIndicatorService<EMAResult>, EMAService>();
builder.Services.AddScoped<IIndicatorService<RSIResult>, RSIService>();
builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();

builder.Services.Scan(scan => scan
    .FromApplicationDependencies()
    .AddClasses(c => c.AssignableTo<IAnalyzer>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());


var host = builder.Build();
host.Run();
