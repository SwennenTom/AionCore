using AionCoreBot;
using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Services;
using AionCoreBot.Domain.Interfaces;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Clients;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Infrastructure.Interfaces;
using AionCoreBot.Infrastructure.Repositories;
using AionCoreBot.Infrastructure.Websocket;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Indicators;
using AionCoreBot.Worker.Interfaces;
using AionCoreBot.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<BotWorker>();
builder.Services.AddScoped<IAnalyzerWorker, AnalyzerWorker>();
builder.Services.AddHostedService<ScopedWorkerHostedService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.EnableSensitiveDataLogging()
        .UseSqlite(builder.Configuration.GetConnectionString("DatabaseConnection")));
builder.Services.AddTransient<CandleAggregator>();
builder.Services.AddScoped<BinanceWebSocketService>();
builder.Services.AddScoped<ICandleRepository, CandleRepository>();
builder.Services.AddScoped<ICandleDownloadService, BinanceCandleDownloadService>();
builder.Services.AddScoped<IIndicatorRepository<EMAResult>, EMARepository>();
builder.Services.AddScoped<IIndicatorRepository<RSIResult>, RSIRepository>();
builder.Services.AddScoped<IIndicatorRepository<ATRResult>, ATRRepository>();
builder.Services.AddScoped<IATRService, ATRService>();
builder.Services.AddScoped<IEMAService, EMAService>();
builder.Services.AddScoped<IRSIService, RSIService>();
builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();
builder.Services.AddScoped<IBinanceRestClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["BinanceAccount:ApiKey"];
    var apiSecret = config["BinanceAccount:ApiSecret"];
    return new BinanceRestClient(apiKey, apiSecret);
});
builder.Services.AddScoped<CandleAggregator>();



var host = builder.Build();
host.Run();
