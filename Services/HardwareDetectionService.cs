using LicenseProtection.Models;
using System;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace LicenseProtection.Services
{
    public class HardwareDetectionService : IHardwareDetectionService
    {
        private ManagementEventWatcher? _insertWatcher;
        private ManagementEventWatcher? _removeWatcher;
        private Timer? _pollTimer;
        private HardwareInfo? _currentHardware;
        private bool _isRunning;

        public event EventHandler<HardwareInfo>? HardwareDetected;
        public event EventHandler? HardwareDisconnected;

        public bool IsHardwareConnected => _currentHardware?.IsConnected == true;

        public async Task<bool> StartDetectionAsync()
        {
            try
            {
                await StopDetectionAsync();

                _insertWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));
                _insertWatcher.EventArrived += OnDeviceInserted;
                _insertWatcher.Start();

                _removeWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3"));
                _removeWatcher.EventArrived += OnDeviceRemoved;
                _removeWatcher.Start();

                _pollTimer = new Timer(CheckHardwareStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

                _isRunning = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task StopDetectionAsync()
        {
            _isRunning = false;

            _insertWatcher?.Stop();
            _insertWatcher?.Dispose();
            _insertWatcher = null;

            _removeWatcher?.Stop();
            _removeWatcher?.Dispose();
            _removeWatcher = null;

            _pollTimer?.Dispose();
            _pollTimer = null;

            await Task.CompletedTask;
        }

        public async Task<HardwareInfo?> GetConnectedHardwareAsync()
        {
            try
            {
                Console.WriteLine("开始检测硬件设备...");
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 2 OR DriveType = 3");
                var drives = searcher.Get();
                
                Console.WriteLine($"找到 {drives.Count} 个可移动设备");

                foreach (ManagementObject drive in drives)
                {
                    var deviceId = drive["DeviceID"]?.ToString();
                    var volumeName = drive["VolumeName"]?.ToString();
                    var driveType = drive["DriveType"]?.ToString();
                    
                    Console.WriteLine($"检查设备: {deviceId}, 名称: {volumeName}, 类型: {driveType}");

                    if (!string.IsNullOrEmpty(deviceId) && IsValidLicenseDevice(deviceId))
                    {
                        Console.WriteLine($"✓ 找到有效的授权设备: {deviceId}");
                        return new HardwareInfo
                        {
                            DeviceId = deviceId,
                            DeviceName = volumeName ?? "License Device",
                            IsConnected = true,
                            LastDetected = DateTime.Now
                        };
                    }
                }

                return await Task.FromResult<HardwareInfo?>(null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                var hardware = await GetConnectedHardwareAsync();
                if (hardware != null)
                {
                    _currentHardware = hardware;
                    HardwareDetected?.Invoke(this, hardware);
                }
            });
        }

        private void OnDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                var hardware = await GetConnectedHardwareAsync();
                if (hardware == null && _currentHardware != null)
                {
                    _currentHardware = null;
                    HardwareDisconnected?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private async void CheckHardwareStatus(object? state)
        {
            if (!_isRunning) return;

            try
            {
                var hardware = await GetConnectedHardwareAsync();
                
                if (hardware != null && _currentHardware == null)
                {
                    _currentHardware = hardware;
                    HardwareDetected?.Invoke(this, hardware);
                }
                else if (hardware == null && _currentHardware != null)
                {
                    _currentHardware = null;
                    HardwareDisconnected?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
            }
        }

        private bool IsValidLicenseDevice(string deviceId)
        {
            try
            {
                var path = Path.Combine(deviceId + "\\", "license.dat");
                Console.WriteLine($"检查路径: {path}");
                var exists = System.IO.File.Exists(path);
                Console.WriteLine($"文件存在: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查失败: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            Task.Run(async () => await StopDetectionAsync());
        }
    }
}