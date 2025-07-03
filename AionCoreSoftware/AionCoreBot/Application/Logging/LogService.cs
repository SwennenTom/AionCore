using AionCoreBot.Domain.Models;
using AionCoreBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AionCoreBot.Application.Logging
{
    public class LogService : ILogService
    {
        // Simpele in-memory opslag (vervang dit met DB-context indien gewenst)
        private readonly List<LogEntry> _logEntries = new();

        public async Task LogAsync(string message, LogClass logLevel = LogClass.Info, string sourceComponent = "", string? exceptionDetails = null)
        {
            var entry = new LogEntry(message, logLevel, sourceComponent, exceptionDetails);
            await SaveAsync(entry);
        }

        public Task SaveAsync(LogEntry logEntry)
        {
            _logEntries.Add(logEntry);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? from = null, DateTime? to = null, LogClass? logLevel = null, string? sourceComponent = "", int? pageNumber = null, int? pageSize = null)
        {
            IEnumerable<LogEntry> result = _logEntries;

            if (from.HasValue)
                result = result.Where(l => l.Timestamp >= from.Value);
            if (to.HasValue)
                result = result.Where(l => l.Timestamp <= to.Value);
            if (logLevel.HasValue)
                result = result.Where(l => l.LogLevel == logLevel.Value);
            if (!string.IsNullOrWhiteSpace(sourceComponent))
                result = result.Where(l => l.SourceComponent == sourceComponent);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                int skip = (pageNumber.Value - 1) * pageSize.Value;
                result = result.Skip(skip).Take(pageSize.Value);
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100, LogClass? logLevel = null, string? sourceComponent = "")
        {
            IEnumerable<LogEntry> result = _logEntries.OrderByDescending(l => l.Timestamp);

            if (logLevel.HasValue)
                result = result.Where(l => l.LogLevel == logLevel.Value);
            if (!string.IsNullOrWhiteSpace(sourceComponent))
                result = result.Where(l => l.SourceComponent == sourceComponent);

            return Task.FromResult(result.Take(count));
        }

        public Task DeleteOldLogsAsync(DateTime before)
        {
            _logEntries.RemoveAll(l => l.Timestamp < before);
            return Task.CompletedTask;
        }
    }
}
