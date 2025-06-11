using AionCoreBot;
using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Services;
using AionCoreBot.Domain.Models;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Indicators;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<IIndicatorService<EMAResult>, EMAService>();
builder.Services.AddScoped<IIndicatorService<RSIResult>, RSIService>();
builder.Services.AddScoped<IIndicatorService<ATRResult>, ATRService>();

builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();


var host = builder.Build();
host.Run();
