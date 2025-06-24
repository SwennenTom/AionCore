using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AionCoreBot.Domain.Enums;

namespace AionCoreBot.Domain.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerMillisecond));
        public LogClass LogLevel { get; set; } = LogClass.Info;
        public string SourceComponent { get; set; } // e.g., "AionCoreBot.Domain.Models.LogEntry"
        public string ExceptionDetails { get; set; } // Optional, for error logs
        private LogEntry() { } // EF Core requires a parameterless constructor
        public LogEntry(string message, LogClass logLevel, string source, string exceptionDetails = null)
        {
            Message = message;
            LogLevel = logLevel;
            SourceComponent = source;
            ExceptionDetails = exceptionDetails;
        }
    }
}
