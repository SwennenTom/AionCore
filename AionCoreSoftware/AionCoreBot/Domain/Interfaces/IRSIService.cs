using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Interfaces
{
    public interface IRSIService
    {
        Task<RSIResult> CalculateAsync(string symbol, string interval, int period, DateTime? startTime = null, DateTime? endTime = null);
       
    }
}
