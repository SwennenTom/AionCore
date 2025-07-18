﻿using AionCoreBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AionCoreBot.Infrastructure.Interfaces
{
    public interface ITradeRepository
    {
        Task<Trade?> GetByIdAsync(int id);
        Task<IEnumerable<Trade>> GetAllAsync();
        Task<IEnumerable<Trade>> GetOpenTradesAsync();
        Task AddAsync(Trade entity);
        void Update(Trade entity);
        void Delete(Trade entity);
        Task SaveChangesAsync();
    }
}
