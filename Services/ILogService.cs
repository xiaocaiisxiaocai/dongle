using System;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILogService
    {
        void LogDebug(string message, string? source = null);
        void LogInfo(string message, string? source = null);
        void LogWarning(string message, string? source = null);
        void LogError(string message, Exception? exception = null, string? source = null);
        void LogCritical(string message, Exception? exception = null, string? source = null);
        
        Task<string[]> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? level = null);
        Task ClearLogsAsync();
        Task ExportLogsAsync(string filePath, DateTime? fromDate = null, DateTime? toDate = null);
        
        event EventHandler<LogEntryEventArgs> LogEntryAdded;
    }

    public class LogEntryEventArgs : EventArgs
    {
        public LogEntry Entry { get; }
        
        public LogEntryEventArgs(LogEntry entry)
        {
            Entry = entry;
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
    }
}