using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Interfaces
{
    public interface IAnalyzer<TInput, TOutput>
    {
        string Name { get; }
        Task<TOutput> AnalyzeAsync(TInput input);
        void ResetState(); // optioneel
    }

}
