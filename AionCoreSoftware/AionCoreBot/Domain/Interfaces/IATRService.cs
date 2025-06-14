using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Interfaces
{
    public interface IATRService
    {
        Task<ATRResult> CalculateAsync(string symbol, string interval, int period, DateTime startTime, DateTime endTime);
        
    }
}
