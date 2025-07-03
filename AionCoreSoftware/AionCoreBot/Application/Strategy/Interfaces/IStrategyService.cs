using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Strategy.Interfaces
{
    public interface IStrategyService
    {
        Task ExecuteStrategyAsync(CancellationToken cancellationToken = default);
    }

}
