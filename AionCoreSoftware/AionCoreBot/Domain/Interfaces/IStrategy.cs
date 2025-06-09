using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Interfaces
{
    public interface IStrategy
    {
        string Name { get; }

        TradeAction Decide(IDictionary<string, object> analyzerOutputs);

        void ResetState();
    }
}
