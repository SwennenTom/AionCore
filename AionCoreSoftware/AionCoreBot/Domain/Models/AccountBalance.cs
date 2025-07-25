﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Domain.Models
{
    public class AccountBalance
    {
        [Key]
        public int Id { get; set; }

        public string Asset { get; set; } = null!; // bv. "EUR", "USDT", "BTC"
        public decimal Amount { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

}
