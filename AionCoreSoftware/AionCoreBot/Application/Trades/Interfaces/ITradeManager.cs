using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AionCoreBot.Application.Trades.Interfaces
{
    public interface ITradeManager
    {
        /// <summary>
        /// Opent een nieuwe trade gebaseerd op de trade beslissing en marktdata.
        /// </summary>
        Task<Trade> OpenTradeAsync(TradeDecision decision, decimal executionPrice);

        /// <summary>
        /// Sluit een openstaande trade (gebaseerd op exit actie).
        /// </summary>
        Task<Trade> CloseTradeAsync(Trade trade, TradeAction exitAction, decimal executionPrice);

        /// <summary>
        /// Haalt openstaande trades op (indien van toepassing).
        /// </summary>
        Task<IReadOnlyList<Trade>> GetOpenTradesAsync();

        /// <summary>
        /// Update trade status en data na order uitvoering.
        /// </summary>
        Task UpdateTradeAsync(Trade trade);
    }
}
