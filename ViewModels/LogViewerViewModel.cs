using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using LicenseProtection.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows;

namespace LicenseProtection.ViewModels
{
    public partial class LogViewerViewModel : ObservableObject
    {
        private readonly ILogService _logService;
        private readonly DispatcherTimer _autoRefreshTimer;

        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();

        [ObservableProperty]
        private DateTime? _fromDate;

        [ObservableProperty]
        private DateTime? _toDate;

        [ObservableProperty]
        private LogLevel? _selectedLogLevel;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private DateTime _lastUpdateTime = DateTime.Now;

        [ObservableProperty]
        private bool _isAutoRefreshEnabled;

        [ObservableProperty]
        private bool _isAutoScrollEnabled = true;

        public LogLevel?[] LogLevels { get; } = new LogLevel?[]
        {
            null, // 全部
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical
        };

        public ICommand RefreshLogsCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand ExportLogsCommand { get; }
        public ICommand ToggleAutoRefreshCommand { get; }

        public LogViewerViewModel(ILogService logService)
        {
            _logService = logService;
            
            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(2);
            _autoRefreshTimer.Tick += async (s, e) => await RefreshLogsAsync();

            RefreshLogsCommand = new AsyncRelayCommand(RefreshLogsAsync);
            ClearLogsCommand = new AsyncRelayCommand(ClearLogsAsync);
            ExportLogsCommand = new AsyncRelayCommand(ExportLogsAsync);
            ToggleAutoRefreshCommand = new RelayCommand(ToggleAutoRefresh);

            // 监听新日志事件
            _logService.LogEntryAdded += OnLogEntryAdded;

            // 初始化日期筛选（最近24小时）
            FromDate = DateTime.Now.AddDays(-1);
            ToDate = DateTime.Now;
        }

        public async Task InitializeAsync()
        {
            await RefreshLogsAsync();
            IsAutoRefreshEnabled = true;
            _autoRefreshTimer.Start();
        }

        private async Task RefreshLogsAsync()
        {
            try
            {
                StatusMessage = "正在加载日志...";
                var logs = await _logService.GetLogsAsync(FromDate, ToDate, SelectedLogLevel);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Clear();
                    foreach (var log in logs)
                    {
                        LogEntries.Add(log);
                    }
                });

                LastUpdateTime = DateTime.Now;
                StatusMessage = $"已加载 {logs.Length} 条日志";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载日志失败: {ex.Message}";
            }
        }

        private async Task ClearLogsAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "确认要清空所有日志吗？此操作不可撤销。",
                    "确认清空",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusMessage = "正在清空日志...";
                    await _logService.ClearLogsAsync();
                    
                    LogEntries.Clear();
                    StatusMessage = "日志已清空";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"清空日志失败: {ex.Message}";
            }
        }

        private async Task ExportLogsAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出日志文件",
                    Filter = "日志文件|*.log|文本文件|*.txt|所有文件|*.*",
                    FileName = $"license_protection_logs_{DateTime.Now:yyyyMMdd_HHmmss}.log"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "正在导出日志...";
                    await _logService.ExportLogsAsync(saveFileDialog.FileName, FromDate, ToDate);
                    StatusMessage = $"日志已导出到: {saveFileDialog.FileName}";
                    
                    // 询问是否打开文件
                    var result = MessageBox.Show(
                        "日志导出成功！是否要打开文件？",
                        "导出完成",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出日志失败: {ex.Message}";
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleAutoRefresh()
        {
            IsAutoRefreshEnabled = !IsAutoRefreshEnabled;
            
            if (IsAutoRefreshEnabled)
            {
                _autoRefreshTimer.Start();
                StatusMessage = "已启用自动刷新";
            }
            else
            {
                _autoRefreshTimer.Stop();
                StatusMessage = "已停止自动刷新";
            }
        }

        private void OnLogEntryAdded(object? sender, LogEntryEventArgs e)
        {
            // 在UI线程中添加新日志
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 检查是否符合当前筛选条件
                    if (FromDate.HasValue && e.Entry.Timestamp < FromDate.Value) return;
                    if (ToDate.HasValue && e.Entry.Timestamp > ToDate.Value) return;
                    if (SelectedLogLevel.HasValue && e.Entry.Level != SelectedLogLevel.Value) return;

                    var logLine = FormatLogEntry(e.Entry);
                    LogEntries.Insert(0, logLine); // 插入到顶部显示最新日志

                    // 限制显示的日志数量
                    while (LogEntries.Count > 1000)
                    {
                        LogEntries.RemoveAt(LogEntries.Count - 1);
                    }

                    LastUpdateTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"添加日志条目失败: {ex.Message}");
                }
            });
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = entry.Level.ToString().ToUpper().PadRight(8);
            var source = entry.Source?.PadRight(20) ?? "Unknown".PadRight(20);
            
            var logLine = $"[{timestamp}] [{level}] [{source}] {entry.Message}";
            
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                logLine += $" | Exception: {entry.Exception}";
            }

            return logLine;
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            // 当筛选条件改变时自动刷新
            if (e.PropertyName == nameof(FromDate) || 
                e.PropertyName == nameof(ToDate) || 
                e.PropertyName == nameof(SelectedLogLevel))
            {
                Task.Run(RefreshLogsAsync);
            }
        }

        public void Dispose()
        {
            _autoRefreshTimer?.Stop();
            _logService.LogEntryAdded -= OnLogEntryAdded;
        }
    }
}