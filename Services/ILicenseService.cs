using LicenseProtection.Models;
using System;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public interface ILicenseService
    {
        Task<LicenseInfo?> LoadLicenseAsync(string devicePath);
        Task<bool> ValidateLicenseAsync(LicenseInfo license);
        Task<bool> UpdateUsageAsync(LicenseInfo license, TimeSpan usageTime);
        Task<bool> SaveLicenseAsync(string devicePath, LicenseInfo license);
        bool IsLicenseExpired(LicenseInfo license);
        bool IsUsageLimitExceeded(LicenseInfo license);
        TimeSpan GetRemainingTime(LicenseInfo license);
    }
}