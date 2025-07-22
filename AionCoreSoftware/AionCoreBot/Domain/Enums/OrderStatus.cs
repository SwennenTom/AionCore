using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Enums
{
    public enum OrderStatus
    {
        Pending,
        Filled,
        Cancelled,
        Rejected,
        Closed
    }
}
