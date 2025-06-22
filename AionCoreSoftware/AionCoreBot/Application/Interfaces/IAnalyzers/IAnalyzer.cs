using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces.IAnalyzers
{
    public interface IAnalyzer
    {
        Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval);
        Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval, DateTime evaluationTime);
    }
}
