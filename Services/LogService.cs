using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

namespace LicenseProtection.Services
{
    public class LogService : ILogService
    {
        private readonly string _logDirectory;
        private readonly string _logFileName;
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly object _writeLock = new();
        private readonly int _maxLogEntries = 10000; // 最大日志条数

        public event EventHandler<LogEntryEventArgs>? LogEntryAdded;

        public LogService()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _logFileName = $"license_protection_{DateTime.Now:yyyyMMdd}.log";
            
            // 确保日志目录存在
            Directory.CreateDirectory(_logDirectory);
        }

        public void LogDebug(string message, string? source = null)
        {
            WriteLog(LogLevel.Debug, message, null, source);
        }

        public void LogInfo(string message, string? source = null)
        {
            WriteLog(LogLevel.Info, message, null, source);
        }

        public void LogWarning(string message, string? source = null)
        {
            WriteLog(LogLevel.Warning, message, null, source);
        }

        public void LogError(string message, Exception? exception = null, string? source = null)
        {
            WriteLog(LogLevel.Error, message, exception, source);
        }

        public void LogCritical(string message, Exception? exception = null, string? source = null)
        {
            WriteLog(LogLevel.Critical, message, exception, source);
        }

        private void WriteLog(LogLevel level, string message, Exception? exception = null, string? source = null)
        {
            try
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message,
                    Source = source ?? GetCallerMethod(),
                    Exception = exception?.Message,
                    StackTrace = exception?.StackTrace
                };

                // 添加到内存队列
                _logQueue.Enqueue(entry);
                
                // 限制内存中的日志数量
                while (_logQueue.Count > _maxLogEntries)
                {
                    _logQueue.TryDequeue(out _);
                }

                // 写入文件
                Task.Run(() => WriteToFileAsync(entry));

                // 触发事件
                LogEntryAdded?.Invoke(this, new LogEntryEventArgs(entry));
            }
            catch (Exception ex)
            {
                // 日志系统本身出错时，写入系统事件日志
                System.Diagnostics.Debug.WriteLine($"日志记录失败: {ex.Message}");
            }
        }

        private async Task WriteToFileAsync(LogEntry entry)
        {
            try
            {
                var logFilePath = Path.Combine(_logDirectory, _logFileName);
                var logLine = FormatLogEntry(entry);

                lock (_writeLock)
                {
                    File.AppendAllText(logFilePath, logLine + Environment.NewLine);
                }

                // 检查日志文件大小，超过10MB则归档
                var fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
                {
                    await ArchiveLogFileAsync(logFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var level = entry.Level.ToString().ToUpper().PadRight(8);
            var source = entry.Source?.PadRight(20) ?? "Unknown".PadRight(20);
            
            var logLine = $"[{timestamp}] [{level}] [{source}] {entry.Message}";
            
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                logLine += $" | Exception: {entry.Exception}";
            }
            
            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                logLine += $" | StackTrace: {entry.StackTrace.Replace(Environment.NewLine, " | ")}";
            }

            return logLine;
        }

        private async Task ArchiveLogFileAsync(string currentLogPath)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                var archivePath = Path.Combine(_logDirectory, $"license_protection_{timestamp}.archived.log");
                
                File.Move(currentLogPath, archivePath);
                
                // 清理旧的归档文件（保留最近30个）
                await CleanupOldLogsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"归档日志文件失败: {ex.Message}");
            }
        }

        private async Task CleanupOldLogsAsync()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.archived.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(30) // 保留最近30个文件
                    .ToArray();

                foreach (var file in logFiles)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // 忽略删除失败的文件
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理旧日志失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        public async Task<string[]> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? level = null)
        {
            try
            {
                var logs = _logQueue.ToArray();
                
                if (fromDate.HasValue)
                {
                    logs = logs.Where(l => l.Timestamp >= fromDate.Value).ToArray();
                }
                
                if (toDate.HasValue)
                {
                    logs = logs.Where(l => l.Timestamp <= toDate.Value).ToArray();
                }
                
                if (level.HasValue)
                {
                    logs = logs.Where(l => l.Level == level.Value).ToArray();
                }

                return logs.OrderByDescending(l => l.Timestamp)
                    .Select(FormatLogEntry)
                    .ToArray();
            }
            catch (Exception ex)
            {
                return new[] { $"获取日志失败: {ex.Message}" };
            }
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                // 清空内存队列
                while (_logQueue.TryDequeue(out _)) { }
                
                // 删除当前日志文件
                var currentLogPath = Path.Combine(_logDirectory, _logFileName);
                if (File.Exists(currentLogPath))
                {
                    File.Delete(currentLogPath);
                }
                
                LogInfo("日志已清空", "LogService");
            }
            catch (Exception ex)
            {
                LogError("清空日志失败", ex, "LogService");
            }

            await Task.CompletedTask;
        }

        public async Task ExportLogsAsync(string filePath, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var logs = await GetLogsAsync(fromDate, toDate);
                await File.WriteAllLinesAsync(filePath, logs);
                LogInfo($"日志已导出到: {filePath}", "LogService");
            }
            catch (Exception ex)
            {
                LogError("导出日志失败", ex, "LogService");
                throw;
            }
        }

        private string GetCallerMethod()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(3); // 跳过当前方法和调用链
                var method = frame?.GetMethod();
                return $"{method?.DeclaringType?.Name}.{method?.Name}";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}