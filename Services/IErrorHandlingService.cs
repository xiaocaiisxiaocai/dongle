using System;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public interface IErrorHandlingService
    {
        void HandleException(Exception exception, string? context = null, bool showToUser = true);
        Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, T? defaultValue = default);
        Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName);
        T ExecuteWithErrorHandling<T>(Func<T> operation, string operationName, T? defaultValue = default);
        void ExecuteWithErrorHandling(Action operation, string operationName);
        
        event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;
    }

    public class ErrorOccurredEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Context { get; }
        public DateTime Timestamp { get; }
        public bool IsHandled { get; set; }

        public ErrorOccurredEventArgs(Exception exception, string context)
        {
            Exception = exception;
            Context = context;
            Timestamp = DateTime.Now;
            IsHandled = false;
        }
    }

    public class ErrorInfo
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
        public string TechnicalMessage { get; set; } = string.Empty;
        public ErrorSeverity Severity { get; set; }
        public string? SuggestedAction { get; set; }
    }

    public enum ErrorSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}