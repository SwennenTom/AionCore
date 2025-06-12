using AionCoreBot;
using AionCoreBot.Application.Interfaces;
using AionCoreBot.Application.Services;
using AionCoreBot.Domain.Models;
using AionCoreBot.Infrastructure.Data;
using AionCoreBot.Worker;
using AionCoreBot.Worker.Indicators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DatabaseConnection")));

//builder.Services.AddScoped<IIndicatorService<EMAResult>, EMAService>();
//builder.Services.AddScoped<IIndicatorService<RSIResult>, RSIService>();
//builder.Services.AddScoped<IIndicatorService<ATRResult>, ATRService>();

builder.Services.AddScoped<ISignalEvaluatorService, SignalEvaluatorService>();


var host = builder.Build();
host.Run();
