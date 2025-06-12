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
        public interface ISignalEvaluatorService
        {
            Task<List<SignalEvaluationResult>> EvaluateSignalsAsync(string symbol, string interval);
        }

    }
}
