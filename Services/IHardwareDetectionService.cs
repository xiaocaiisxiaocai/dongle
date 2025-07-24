using LicenseProtection.Models;
using System;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public interface IHardwareDetectionService
    {
        event EventHandler<HardwareInfo>? HardwareDetected;
        event EventHandler? HardwareDisconnected;
        
        Task<bool> StartDetectionAsync();
        Task StopDetectionAsync();
        Task<HardwareInfo?> GetConnectedHardwareAsync();
        bool IsHardwareConnected { get; }
    }
}