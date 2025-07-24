using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace LicenseProtection.Services
{
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogService _logService;
        private readonly Dictionary<Type, ErrorInfo> _errorMappings;

        public event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;

        public ErrorHandlingService(ILogService logService)
        {
            _logService = logService;
            _errorMappings = InitializeErrorMappings();
        }

        public void HandleException(Exception exception, string? context = null, bool showToUser = true)
        {
            try
            {
                var errorInfo = GetErrorInfo(exception);
                var fullContext = context ?? "未知操作";

                // 记录日志
                var logLevel = GetLogLevelFromSeverity(errorInfo.Severity);
                switch (logLevel)
                {
                    case LogLevel.Error:
                        _logService.LogError($"{fullContext}: {errorInfo.TechnicalMessage}", exception, "ErrorHandler");
                        break;
                    case LogLevel.Critical:
                        _logService.LogCritical($"{fullContext}: {errorInfo.TechnicalMessage}", exception, "ErrorHandler");
                        break;
                    default:
                        _logService.LogWarning($"{fullContext}: {errorInfo.TechnicalMessage}", "ErrorHandler");
                        break;
                }

                // 触发错误事件
                var errorArgs = new ErrorOccurredEventArgs(exception, fullContext);
                ErrorOccurred?.Invoke(this, errorArgs);

                // 显示用户友好的错误信息
                if (showToUser && !errorArgs.IsHandled)
                {
                    ShowUserFriendlyError(errorInfo, fullContext);
                }
            }
            catch (Exception handlingException)
            {
                // 处理错误处理本身的异常
                _logService.LogCritical("错误处理服务本身发生异常", handlingException, "ErrorHandler");
                
                // 显示基本错误信息
                try
                {
                    MessageBox.Show(
                        $"系统发生严重错误，请联系技术支持。\n\n错误详情: {exception.Message}",
                        "系统错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch
                {
                    // 最后的安全网
                    System.Diagnostics.Debug.WriteLine($"Critical error: {exception}");
                }
            }
        }

        public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, T? defaultValue = default)
        {
            try
            {
                _logService.LogDebug($"开始执行操作: {operationName}", "ErrorHandler");
                var result = await operation();
                _logService.LogDebug($"操作执行成功: {operationName}", "ErrorHandler");
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, true);
                return defaultValue!;
            }
        }

        public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                _logService.LogDebug($"开始执行操作: {operationName}", "ErrorHandler");
                await operation();
                _logService.LogDebug($"操作执行成功: {operationName}", "ErrorHandler");
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, true);
            }
        }

        public T ExecuteWithErrorHandling<T>(Func<T> operation, string operationName, T? defaultValue = default)
        {
            try
            {
                _logService.LogDebug($"开始执行操作: {operationName}", "ErrorHandler");
                var result = operation();
                _logService.LogDebug($"操作执行成功: {operationName}", "ErrorHandler");
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, true);
                return defaultValue!;
            }
        }

        public void ExecuteWithErrorHandling(Action operation, string operationName)
        {
            try
            {
                _logService.LogDebug($"开始执行操作: {operationName}", "ErrorHandler");
                operation();
                _logService.LogDebug($"操作执行成功: {operationName}", "ErrorHandler");
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, true);
            }
        }

        private ErrorInfo GetErrorInfo(Exception exception)
        {
            var exceptionType = exception.GetType();
            
            if (_errorMappings.TryGetValue(exceptionType, out var errorInfo))
            {
                return errorInfo;
            }

            // 根据异常类型返回通用错误信息
            return exception switch
            {
                UnauthorizedAccessException => new ErrorInfo
                {
                    ErrorCode = "ACCESS_DENIED",
                    UserMessage = "没有足够的权限执行此操作",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.High,
                    SuggestedAction = "请以管理员身份运行程序，或检查文件/文件夹权限"
                },
                System.IO.FileNotFoundException => new ErrorInfo
                {
                    ErrorCode = "FILE_NOT_FOUND",
                    UserMessage = "找不到所需的文件",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请确认文件路径是否正确，或重新安装程序"
                },
                System.IO.DirectoryNotFoundException => new ErrorInfo
                {
                    ErrorCode = "DIRECTORY_NOT_FOUND",
                    UserMessage = "找不到指定的目录",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请确认路径是否正确，或手动创建目录"
                },
                System.Net.NetworkInformation.NetworkInformationException => new ErrorInfo
                {
                    ErrorCode = "NETWORK_ERROR",
                    UserMessage = "网络连接失败",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请检查网络连接或联系网络管理员"
                },
                OutOfMemoryException => new ErrorInfo
                {
                    ErrorCode = "OUT_OF_MEMORY",
                    UserMessage = "系统内存不足",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.Critical,
                    SuggestedAction = "请关闭其他程序释放内存，或增加系统内存"
                },
                _ => new ErrorInfo
                {
                    ErrorCode = "UNKNOWN_ERROR",
                    UserMessage = "发生未知错误",
                    TechnicalMessage = exception.Message,
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请重试操作，如问题持续存在请联系技术支持"
                }
            };
        }

        private Dictionary<Type, ErrorInfo> InitializeErrorMappings()
        {
            return new Dictionary<Type, ErrorInfo>
            {
                [typeof(InvalidOperationException)] = new ErrorInfo
                {
                    ErrorCode = "INVALID_OPERATION",
                    UserMessage = "操作无法在当前状态下执行",
                    TechnicalMessage = "当前状态不允许执行此操作",
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请检查操作的前置条件是否满足"
                },
                [typeof(ArgumentException)] = new ErrorInfo
                {
                    ErrorCode = "INVALID_ARGUMENT",
                    UserMessage = "输入的参数不正确",
                    TechnicalMessage = "提供的参数无效",
                    Severity = ErrorSeverity.Low,
                    SuggestedAction = "请检查输入的信息是否正确"
                },
                [typeof(TimeoutException)] = new ErrorInfo
                {
                    ErrorCode = "TIMEOUT",
                    UserMessage = "操作超时",
                    TechnicalMessage = "操作执行时间过长",
                    Severity = ErrorSeverity.Medium,
                    SuggestedAction = "请重试操作，或检查网络连接"
                }
            };
        }

        private LogLevel GetLogLevelFromSeverity(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Low => LogLevel.Warning,
                ErrorSeverity.Medium => LogLevel.Error,
                ErrorSeverity.High => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Error
            };
        }

        private void ShowUserFriendlyError(ErrorInfo errorInfo, string context)
        {
            try
            {
                var icon = errorInfo.Severity switch
                {
                    ErrorSeverity.Low => MessageBoxImage.Information,
                    ErrorSeverity.Medium => MessageBoxImage.Warning,
                    ErrorSeverity.High => MessageBoxImage.Error,
                    ErrorSeverity.Critical => MessageBoxImage.Stop,
                    _ => MessageBoxImage.Warning
                };

                var title = errorInfo.Severity switch
                {
                    ErrorSeverity.Low => "提示",
                    ErrorSeverity.Medium => "警告",
                    ErrorSeverity.High => "错误",
                    ErrorSeverity.Critical => "严重错误",
                    _ => "系统消息"
                };

                var message = $"{errorInfo.UserMessage}";
                
                if (!string.IsNullOrEmpty(errorInfo.SuggestedAction))
                {
                    message += $"\n\n建议: {errorInfo.SuggestedAction}";
                }

                if (errorInfo.Severity >= ErrorSeverity.High)
                {
                    message += $"\n\n错误代码: {errorInfo.ErrorCode}";
                    message += $"\n操作: {context}";
                }

                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                _logService.LogError("显示错误消息失败", ex, "ErrorHandler");
            }
        }
    }
}