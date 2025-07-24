using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using LicenseProtection.Models;
using LicenseProtection.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace LicenseProtection.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IHardwareDetectionService _hardwareService;
        private readonly ILicenseService _licenseService;
        private readonly ILogService _logService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly DispatcherTimer _usageTimer;
        private DateTime _sessionStartTime;

        [ObservableProperty]
        private string _deviceStatus = "未检测到设备";

        [ObservableProperty]
        private string _licenseStatus = "未授权";

        [ObservableProperty]
        private string _customerName = "";

        [ObservableProperty]
        private string _productName = "";

        [ObservableProperty]
        private string _serialNumber = "";

        [ObservableProperty]
        private string _expirationDate = "";

        [ObservableProperty]
        private string _usageInfo = "";

        [ObservableProperty]
        private string _remainingTime = "";

        [ObservableProperty]
        private bool _isLicenseValid;

        [ObservableProperty]
        private bool _canUseSoftware;

        private LicenseInfo? _currentLicense;
        private HardwareInfo? _currentHardware;

        public MainViewModel(IHardwareDetectionService hardwareService, ILicenseService licenseService, 
            ILogService logService, IErrorHandlingService errorHandlingService)
        {
            _hardwareService = hardwareService;
            _licenseService = licenseService;
            _logService = logService;
            _errorHandlingService = errorHandlingService;

            _hardwareService.HardwareDetected += OnHardwareDetected;
            _hardwareService.HardwareDisconnected += OnHardwareDisconnected;

            _usageTimer = new DispatcherTimer();
            _usageTimer.Interval = TimeSpan.FromMinutes(1);
            _usageTimer.Tick += OnUsageTimerTick;

            StartDetectionCommand = new AsyncRelayCommand(StartDetectionAsync);
            StopDetectionCommand = new AsyncRelayCommand(StopDetectionAsync);
            RefreshLicenseCommand = new AsyncRelayCommand(RefreshLicenseAsync);
            OpenLicenseManagerCommand = new RelayCommand(OpenLicenseManager);
            OpenLogViewerCommand = new RelayCommand(OpenLogViewer);

            _logService.LogInfo("主程序初始化完成", "MainViewModel");
        }

        public ICommand StartDetectionCommand { get; }
        public ICommand StopDetectionCommand { get; }
        public ICommand RefreshLicenseCommand { get; }
        public ICommand OpenLicenseManagerCommand { get; }
        public ICommand OpenLogViewerCommand { get; }

        public async Task InitializeAsync()
        {
            await _hardwareService.StartDetectionAsync();
            await CheckExistingHardwareAsync();
        }

        private async Task StartDetectionAsync()
        {
            DeviceStatus = "正在检测设备...";
            await _hardwareService.StartDetectionAsync();
            
            // 立即检查现有设备
            await CheckExistingHardwareAsync();
        }

        private async Task StopDetectionAsync()
        {
            await _hardwareService.StopDetectionAsync();
            DeviceStatus = "检测已停止";
            ResetLicenseInfo();
        }

        private async Task RefreshLicenseAsync()
        {
            if (_currentHardware != null)
            {
                await LoadLicenseAsync(_currentHardware.DeviceId);
            }
        }

        private void OpenLicenseManager()
        {
            _errorHandlingService.ExecuteWithErrorHandling(() =>
            {
                _logService.LogInfo("打开授权管理器", "MainViewModel");
                var managerWindow = new Views.LicenseManagerWindow();
                managerWindow.Show();
            }, "打开授权管理器");
        }

        private void OpenLogViewer()
        {
            _errorHandlingService.ExecuteWithErrorHandling(() =>
            {
                _logService.LogInfo("打开日志查看器", "MainViewModel");
                var logViewerWindow = new Views.LogViewerWindow(_logService);
                logViewerWindow.Show();
            }, "打开日志查看器");
        }

        private async Task CheckExistingHardwareAsync()
        {
            System.Console.WriteLine("ViewModel: 开始检查现有设备...");
            var hardware = await _hardwareService.GetConnectedHardwareAsync();
            if (hardware != null)
            {
                System.Console.WriteLine($"ViewModel: 找到设备 {hardware.DeviceId}");
                await OnHardwareDetectedAsync(hardware);
            }
            else
            {
                System.Console.WriteLine("ViewModel: 没有找到设备");
                DeviceStatus = "未检测到设备";
            }
        }

        private async void OnHardwareDetected(object? sender, HardwareInfo hardware)
        {
            await OnHardwareDetectedAsync(hardware);
        }

        private async Task OnHardwareDetectedAsync(HardwareInfo hardware)
        {
            System.Console.WriteLine($"ViewModel: 处理设备检测事件 {hardware.DeviceId}");
            _currentHardware = hardware;
            DeviceStatus = $"设备已连接: {hardware.DeviceName}";
            
            System.Console.WriteLine($"ViewModel: 设备状态已更新为: {DeviceStatus}");
            await LoadLicenseAsync(hardware.DeviceId);
        }

        private void OnHardwareDisconnected(object? sender, EventArgs e)
        {
            _currentHardware = null;
            DeviceStatus = "设备已断开连接";
            ResetLicenseInfo();
            StopUsageTracking();
        }

        private async Task LoadLicenseAsync(string deviceId)
        {
            try
            {
                System.Console.WriteLine($"ViewModel: 开始加载授权文件 {deviceId}");
                _currentLicense = await _licenseService.LoadLicenseAsync(deviceId);
                
                if (_currentLicense != null)
                {
                    System.Console.WriteLine($"ViewModel: 授权文件加载成功，客户: {_currentLicense.CustomerName}");
                    await UpdateLicenseDisplayAsync();
                    await ValidateLicenseAsync();
                }
                else
                {
                    System.Console.WriteLine("ViewModel: 授权文件加载失败");
                    LicenseStatus = "未找到授权文件";
                    IsLicenseValid = false;
                    CanUseSoftware = false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ViewModel: 授权文件读取异常: {ex.Message}");
                LicenseStatus = "授权文件读取失败";
                IsLicenseValid = false;
                CanUseSoftware = false;
            }
        }

        private async Task UpdateLicenseDisplayAsync()
        {
            if (_currentLicense == null) return;

            CustomerName = _currentLicense.CustomerName;
            ProductName = _currentLicense.ProductName;
            SerialNumber = _currentLicense.SerialNumber;
            ExpirationDate = _currentLicense.ExpirationDate.ToString("yyyy-MM-dd HH:mm:ss");
            
            var usedHours = _currentLicense.UsedHours;
            var maxHours = _currentLicense.MaxUsageHours;
            
            if (maxHours > 0)
            {
                UsageInfo = $"已使用: {usedHours}/{maxHours} 小时";
                var remaining = _licenseService.GetRemainingTime(_currentLicense);
                RemainingTime = remaining == TimeSpan.MaxValue ? "无限制" : $"{remaining.TotalHours:F1} 小时";
            }
            else
            {
                UsageInfo = $"已使用: {usedHours} 小时";
                RemainingTime = "无限制";
            }

            await Task.CompletedTask;
        }

        private async Task ValidateLicenseAsync()
        {
            if (_currentLicense == null) return;

            var isValid = await _licenseService.ValidateLicenseAsync(_currentLicense);
            
            IsLicenseValid = isValid;
            CanUseSoftware = isValid;

            if (isValid)
            {
                LicenseStatus = "授权有效";
                StartUsageTracking();
            }
            else
            {
                if (_licenseService.IsLicenseExpired(_currentLicense))
                    LicenseStatus = "授权已过期";
                else if (_licenseService.IsUsageLimitExceeded(_currentLicense))
                    LicenseStatus = "使用时长已超限";
                else
                    LicenseStatus = "授权无效";
            }
        }

        private void StartUsageTracking()
        {
            _sessionStartTime = DateTime.Now;
            _usageTimer.Start();
        }

        private void StopUsageTracking()
        {
            _usageTimer.Stop();
            
            if (_currentLicense != null && _sessionStartTime != default)
            {
                var sessionTime = DateTime.Now - _sessionStartTime;
                Task.Run(async () =>
                {
                    await _licenseService.UpdateUsageAsync(_currentLicense, sessionTime);
                    if (_currentHardware != null)
                    {
                        await _licenseService.SaveLicenseAsync(_currentHardware.DeviceId, _currentLicense);
                    }
                });
            }
        }

        private async void OnUsageTimerTick(object? sender, EventArgs e)
        {
            if (_currentLicense != null && _currentHardware != null)
            {
                var sessionTime = DateTime.Now - _sessionStartTime;
                await _licenseService.UpdateUsageAsync(_currentLicense, TimeSpan.FromMinutes(1));
                await _licenseService.SaveLicenseAsync(_currentHardware.DeviceId, _currentLicense);
                await UpdateLicenseDisplayAsync();
                await ValidateLicenseAsync();
            }
        }

        private void ResetLicenseInfo()
        {
            _currentLicense = null;
            LicenseStatus = "未授权";
            CustomerName = "";
            ProductName = "";
            SerialNumber = "";
            ExpirationDate = "";
            UsageInfo = "";
            RemainingTime = "";
            IsLicenseValid = false;
            CanUseSoftware = false;
        }
    }
}