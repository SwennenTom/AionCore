using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IStrategyService
    {
        Task ExecuteStrategyAsync(CancellationToken cancellationToken = default);
    }

}
