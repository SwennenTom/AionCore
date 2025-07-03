using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Domain.Enums;
using AionCoreBot.Domain.Models;

namespace AionCoreBot.Application.Logging
{
    public interface ILogService
    {
        Task LogAsync(string message, LogClass logLevel=LogClass.Info, string sourceComponent="", string? exceptionDetails = null);
        Task SaveAsync(LogEntry logEntry);
        Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? from = null, DateTime? to = null, LogClass? logLevel = null, string? sourceComponent = "", int? pageNumber = null, int? pageSize = null);
        Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100, LogClass? logLevel = null, string? sourceComponent = "");
        Task DeleteOldLogsAsync(DateTime before);
    }
}
