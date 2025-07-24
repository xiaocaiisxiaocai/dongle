using System;

namespace LicenseProtection.Models
{
    public class LicenseInfo
    {
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public int MaxUsageHours { get; set; }
        public int UsedHours { get; set; }
        public bool IsActive { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public LicenseType Type { get; set; }
    }

    public enum LicenseType
    {
        Trial,
        Standard,
        Professional,
        Enterprise
    }

    public class HardwareInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime LastDetected { get; set; }
    }
}