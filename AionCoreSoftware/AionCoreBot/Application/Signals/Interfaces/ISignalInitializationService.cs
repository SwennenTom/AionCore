﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Signals.Interfaces
{
    public interface ISignalInitializationService
    {
        Task EvaluateHistoricalSignalsAsync(CancellationToken cancellationToken = default);
    }
}
