using AionCoreBot;
using AionCoreBot.Application.Indicators;
using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Interfaces.IIndicators;
using AionCoreBot.Application.Services;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Clients;
using AionCoreBot.Infrastructure.Comms.Clients;
using AionCoreBot.Infrastructure.Comms.Websocket;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<BotWorker>();
builder.Services.AddScoped<IAnalyzerWorker, AnalyzerWorker>();
builder.Services.AddHostedService<ScopedWorkerHostedService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options//.EnableSensitiveDataLogging()
        .UseSqlite(builder.Configuration.GetConnectionString("DatabaseConnection")));
builder.Services.AddTransient<CandleAggregator>();
builder.Services.AddScoped<BinanceWebSocketService>();
builder.Services.AddScoped<ICandleRepository, CandleRepository>();
builder.Services.AddScoped<ICandleDownloadService, BinanceCandleDownloadService>();
builder.Services.AddScoped<IIndicatorRepository<EMAResult>, EMARepository>();
builder.Services.AddScoped<IIndicatorRepository<RSIResult>, RSIRepository>();
builder.Services.AddScoped<IIndicatorRepository<ATRResult>, ATRRepository>();
builder.Services.AddScoped<IBaseIndicatorService<EMAResult>, EMAService>();
builder.Services.AddScoped<IBaseIndicatorService<ATRResult>, ATRService>();
builder.Services.AddScoped<IBaseIndicatorService<RSIResult>, RSIService>();
builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();
builder.Services.AddScoped<ISignalEvaluationRepository, SignalEvaluationRepository>();
builder.Services.AddScoped<CandleAggregator>();
builder.Services.AddScoped<IBinanceRestClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["BinanceAccount:ApiKey"];
    var apiSecret = config["BinanceAccount:ApiSecret"];
    return new BinanceRestClient(apiKey, apiSecret);
});

var host = builder.Build();
host.Run();
