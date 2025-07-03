using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IDataCleanupService
    {
        Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    }
}
