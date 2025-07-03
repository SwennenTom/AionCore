using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface ICandleDownloadService
    {
        Task<List<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, DateTime from, DateTime to);
        Task<List<Candle>> DownloadCandlesAsync(string symbol, string interval, int days);

    }

}
