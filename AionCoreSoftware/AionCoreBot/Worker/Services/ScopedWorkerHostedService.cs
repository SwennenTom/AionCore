using AionCoreBot.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Services
{
    public class ScopedWorkerHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ScopedWorkerHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<BotWorker>();
            await worker.RunAsync(stoppingToken);
        }
    }

}
