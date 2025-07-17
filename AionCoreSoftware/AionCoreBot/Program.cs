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
using AionCoreBot.Helpers;
using AionCoreBot.Infrastructure.Comms.Clients;
using AionCoreBot.Infrastructure.Comms.Interfaces;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

// === Lees API keys ===
var apiKey = builder.Configuration["BinanceAccount:ApiKey"];
var apiSecret = builder.Configuration["BinanceAccount:ApiSecret"];

// === Configureer Binance.Net globally ===
BinanceRestClient.SetDefaultOptions(opts =>
{
    opts.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
});
BinanceSocketClient.SetDefaultOptions(opts =>
{
    opts.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
});

// === App services ===
builder.Services.AddScoped<IAccountSyncService, AccountSyncService>();
builder.Services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
builder.Services.AddScoped<IBalanceHistoryRepository, BalanceHistoryRepository>();
builder.Services.AddScoped<IBalanceProvider, BalanceProvider>();

builder.Services.AddScoped<BotWorker>();
builder.Services.AddScoped<CandleAggregator>();
builder.Services.AddScoped<IAnalyzerWorker, AnalyzerWorker>();
builder.Services.AddHostedService<ScopedWorkerHostedService>();
builder.Services.AddHostedService<TrailingStopWorker>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategizer, Strategizer>();
builder.Services.AddScoped<ISignalInitializationService, SignalInitializationService>();
builder.Services.AddScoped<ICandleInitializationService, CandleInitializationService>();
builder.Services.AddScoped<IDataCleanupService, DataCleanupService>();
builder.Services.AddScoped<IRiskManagementService, RiskManagementService>();
builder.Services.AddScoped<ITradeManager, TradeManager>();
builder.Services.AddScoped<TradeDecisionService>();

// === Binance.Net clients als DI services ===
builder.Services.AddScoped<BinanceRestClient>();

builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var key = cfg["BinanceAccount:ApiKey"];
    var secret = cfg["BinanceAccount:ApiSecret"];

    return new BinanceSocketClient(options =>
    {
        options.ApiCredentials = new ApiCredentials(key, secret);
    });
});


// === Order Service (Paper vs Real) ===
builder.Services.AddScoped<IExchangeOrderService, BinanceExchangeOrderService>();

// === Database ===
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DatabaseConnection")));

// === Candle downloaders & websocket ===
builder.Services.AddScoped<BinanceWebSocketService>();
builder.Services.AddScoped<ICandleDownloadService, BinanceCandleDownloadService>();

// === Repositories ===
builder.Services.AddScoped<ICandleRepository, CandleRepository>();
builder.Services.AddScoped<IIndicatorRepository<EMAResult>, EMARepository>();
builder.Services.AddScoped<IIndicatorRepository<RSIResult>, RSIRepository>();
builder.Services.AddScoped<IIndicatorRepository<ATRResult>, ATRRepository>();
builder.Services.AddScoped<ISignalEvaluationRepository, SignalEvaluationRepository>();

// === Indicator services ===
builder.Services.AddScoped<IIndicatorService<ATRResult>, ATRService>();
builder.Services.AddScoped<IIndicatorService<EMAResult>, EMAService>();
builder.Services.AddScoped<IIndicatorService<RSIResult>, RSIService>();
builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();

// === Dynamisch alle analyzers scannen ===
builder.Services.Scan(scan => scan
    .FromApplicationDependencies()
    .AddClasses(c => c.AssignableTo<IAnalyzer>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());

var host = builder.Build();
host.Run();
