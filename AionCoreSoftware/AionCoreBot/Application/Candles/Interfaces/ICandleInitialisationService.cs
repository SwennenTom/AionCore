using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Candles.Interfaces
{
    public interface ICandleInitializationService
    {
        Task DownloadHistoricalCandlesAsync(CancellationToken cancellationToken = default);
    }
}
