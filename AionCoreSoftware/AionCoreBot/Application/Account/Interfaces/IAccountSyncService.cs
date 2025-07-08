using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Account.Interfaces
{
    public interface IAccountSyncService
    {
        Task InitializeAsync();   // Bij opstart
        Task SyncAsync();         // Periodiek (bv. elke minuut)
    }
}
