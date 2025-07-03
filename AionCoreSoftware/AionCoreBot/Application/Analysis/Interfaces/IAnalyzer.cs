using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

<<<<<<<< HEAD:AionCoreSoftware/AionCoreBot/Application/Interfaces/IAnalyzers/IAnalyzer.cs
namespace AionCoreBot.Application.Interfaces.IAnalyzers
========
namespace AionCoreBot.Application.Analysis.Interfaces
>>>>>>>> 726836046968b3e62e75b7e9ac50444e9b4741fc:AionCoreSoftware/AionCoreBot/Application/Analysis/Interfaces/IAnalyzer.cs
{
    public interface IAnalyzer
    {
        Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval);
        Task<SignalEvaluationResult> AnalyzeAsync(string symbol, string interval, DateTime evaluationTime);
    }
}
