using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Worker.Interfaces
{
    public interface IAnalyzerWorker
    {
        Task RunAllAsync();
    }
}
