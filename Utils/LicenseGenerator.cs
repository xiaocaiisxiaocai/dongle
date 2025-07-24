using LicenseProtection.Models;
using LicenseProtection.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LicenseProtection.Utils
{
    public static class LicenseGenerator
    {
        public static async Task<bool> GenerateLicenseFileAsync(string outputPath, LicenseInfo licenseInfo)
        {
            try
            {
                var licenseService = new LicenseService();
                var devicePath = Path.GetDirectoryName(outputPath) ?? "";
                return await licenseService.SaveLicenseAsync(devicePath, licenseInfo);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static LicenseInfo CreateTrialLicense(string customerName, string productName, int trialDays = 30, int maxHours = 100)
        {
            return new LicenseInfo
            {
                SerialNumber = GenerateSerialNumber(),
                CustomerName = customerName,
                ProductName = productName,
                Type = LicenseType.Trial,
                ExpirationDate = DateTime.Now.AddDays(trialDays),
                MaxUsageHours = maxHours,
                UsedHours = 0,
                IsActive = true
            };
        }

        public static LicenseInfo CreateStandardLicense(string customerName, string productName, DateTime expirationDate, int maxHours = 0)
        {
            return new LicenseInfo
            {
                SerialNumber = GenerateSerialNumber(),
                CustomerName = customerName,
                ProductName = productName,
                Type = LicenseType.Standard,
                ExpirationDate = expirationDate,
                MaxUsageHours = maxHours,
                UsedHours = 0,
                IsActive = true
            };
        }

        public static LicenseInfo CreateProfessionalLicense(string customerName, string productName, DateTime expirationDate)
        {
            return new LicenseInfo
            {
                SerialNumber = GenerateSerialNumber(),
                CustomerName = customerName,
                ProductName = productName,
                Type = LicenseType.Professional,
                ExpirationDate = expirationDate,
                MaxUsageHours = 0,
                UsedHours = 0,
                IsActive = true
            };
        }

        public static LicenseInfo CreateEnterpriseLicense(string customerName, string productName)
        {
            return new LicenseInfo
            {
                SerialNumber = GenerateSerialNumber(),
                CustomerName = customerName,
                ProductName = productName,
                Type = LicenseType.Enterprise,
                ExpirationDate = DateTime.MaxValue,
                MaxUsageHours = 0,
                UsedHours = 0,
                IsActive = true
            };
        }

        private static string GenerateSerialNumber()
        {
            var random = new Random();
            var part1 = random.Next(1000, 9999).ToString();
            var part2 = random.Next(1000, 9999).ToString();
            var part3 = random.Next(1000, 9999).ToString();
            var part4 = random.Next(1000, 9999).ToString();
            
            return $"{part1}-{part2}-{part3}-{part4}";
        }
    }
}