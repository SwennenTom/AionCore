using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Interfaces
{
    public interface IRiskManagementService
    {
        /// <summary>
        /// Berekent het maximale risicobedrag (bijv. in account valuta) voor een trade op basis van het symbool, 
        /// huidige positie en risicoparameters.
        /// </summary>
        /// <param name="symbol">Het trading symbool, bv. BTCUSDT.</param>
        /// <param name="currentPositionSize">De huidige positie grootte (positief of negatief).</param>
        /// <returns>Maximaal risicobedrag in geld of percentage, afhankelijk van implementatie.</returns>
        decimal CalculateMaxRiskAmount(string symbol, decimal currentPositionSize);

        /// <summary>
        /// Bepaalt de aanbevolen positie grootte op basis van risico, beschikbare middelen en prijs.
        /// </summary>
        /// <param name="symbol">Het trading symbool.</param>
        /// <param name="riskAmount">Het maximaal toegestane risico (bv. in geld).</param>
        /// <param name="entryPrice">De prijs waartegen de trade geopend zou worden.</param>
        /// <returns>De aanbevolen positie grootte (aantal eenheden of contracts).</returns>
        decimal CalculatePositionSize(string symbol, decimal riskAmount, decimal entryPrice);

        /// <summary>
        /// Controleert of een voorgestelde trade actie binnen de risicogrenzen valt.
        /// </summary>
        /// <param name="symbol">Het trading symbool.</param>
        /// <param name="proposedAction">De voorgestelde trade actie (Buy, Sell, etc.).</param>
        /// <param name="positionSize">De voorgestelde positie grootte.</param>
        /// <returns>True als de trade acceptabel is volgens risk management regels, anders false.</returns>
        bool IsTradeWithinRiskLimits(string symbol, TradeAction proposedAction, decimal positionSize);

        /// <summary>
        /// Optioneel: Update risk parameters, zoals stop loss levels of trailing stops, voor een open positie.
        /// </summary>
        /// <param name="tradeId">De ID van de open trade.</param>
        void UpdateRiskParameters(int tradeId);
    }
}
