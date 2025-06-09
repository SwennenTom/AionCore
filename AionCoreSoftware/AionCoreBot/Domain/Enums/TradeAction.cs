using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Enums
{
    public enum TradeAction
    {
        Hold,
        Buy,
        Sell,
        Short,
        Cover,
        AddToPosition,
        ReducePosition,
        CancelOrder,
        LimitBuy,
        LimitSell,
        StopBuy,
        StopSell,
        TrailStop,
        Liquidate
    }
}
