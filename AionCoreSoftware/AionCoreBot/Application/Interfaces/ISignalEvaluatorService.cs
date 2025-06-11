using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Domain.Models;

namespace AionCoreBot.Application.Interfaces
{
    public interface ISignalEvaluatorService
    {
        Task<SignalEvaluationResult> EvaluateSignalsAsync(string symbol, string interval);
    }
}
