using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AionCoreBot.Infrastructure.Data;

namespace AionCoreBot.Application.Logging
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _dbContext;

        public LogService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogAsync(string message, LogClass logLevel = LogClass.Info, string sourceComponent = "", string? exceptionDetails = null)
        {
            var entry = new LogEntry(message, logLevel, sourceComponent, exceptionDetails);
            await SaveAsync(entry);
        }

        public async Task SaveAsync(LogEntry logEntry)
        {
            await _dbContext.Logs.AddAsync(logEntry);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? from = null, DateTime? to = null, LogClass? logLevel = null, string? sourceComponent = "", int? pageNumber = null, int? pageSize = null)
        {
            IQueryable<LogEntry> query = _dbContext.Logs.AsQueryable();

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);
            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);
            if (logLevel.HasValue)
                query = query.Where(l => l.LogLevel == logLevel.Value);
            if (!string.IsNullOrWhiteSpace(sourceComponent))
                query = query.Where(l => l.SourceComponent == sourceComponent);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                int skip = (pageNumber.Value - 1) * pageSize.Value;
                query = query.Skip(skip).Take(pageSize.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100, LogClass? logLevel = null, string? sourceComponent = "")
        {
            IQueryable<LogEntry> query = _dbContext.Logs
                .OrderByDescending(l => l.Timestamp);

            if (logLevel.HasValue)
                query = query.Where(l => l.LogLevel == logLevel.Value);
            if (!string.IsNullOrWhiteSpace(sourceComponent))
                query = query.Where(l => l.SourceComponent == sourceComponent);

            return await query.Take(count).ToListAsync();
        }

        public async Task DeleteOldLogsAsync(CancellationToken token)
        {
            if(token.IsCancellationRequested)
            {
                var before = DateTime.UtcNow.AddMonths(-6);
                var oldLogs = _dbContext.Logs.Where(l => l.Timestamp < before);
                _dbContext.Logs.RemoveRange(oldLogs);
                await _dbContext.SaveChangesAsync();
            }            
        }
    }
}
