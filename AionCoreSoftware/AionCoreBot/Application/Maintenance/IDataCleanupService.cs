using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Maintenance
{
    public interface IDataCleanupService
    {
        Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    }
}
