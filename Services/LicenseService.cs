using LicenseProtection.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public class LicenseService : ILicenseService
    {
        private const string LicenseFileName = "license.dat";
        private readonly byte[] _encryptionKey = Encoding.UTF8.GetBytes("LicenseProtection2024Key!");

        public async Task<LicenseInfo?> LoadLicenseAsync(string devicePath)
        {
            try
            {
                var licensePath = Path.Combine(devicePath, LicenseFileName);
                Console.WriteLine($"LicenseService: 尝试加载授权文件: {licensePath}");
                
                if (!File.Exists(licensePath))
                {
                    Console.WriteLine("LicenseService: 授权文件不存在");
                    return null;
                }

                var fileData = await File.ReadAllBytesAsync(licensePath);
                string json;
                
                try
                {
                    // 尝试作为加密文件解密
                    var decryptedData = Decrypt(fileData);
                    json = Encoding.UTF8.GetString(decryptedData);
                    Console.WriteLine("LicenseService: 成功解密授权文件");
                }
                catch
                {
                    // 如果解密失败，尝试作为明文JSON读取
                    json = Encoding.UTF8.GetString(fileData);
                    Console.WriteLine("LicenseService: 作为明文JSON读取授权文件");
                }
                
                var license = JsonConvert.DeserializeObject<LicenseInfo>(json);
                Console.WriteLine($"LicenseService: 解析成功，客户: {license?.CustomerName}");
                return license;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LicenseService: 加载失败: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ValidateLicenseAsync(LicenseInfo license)
        {
            if (license == null) return false;

            try
            {
                if (!license.IsActive) return false;
                if (IsLicenseExpired(license)) return false;
                if (IsUsageLimitExceeded(license)) return false;

                return await Task.FromResult(true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUsageAsync(LicenseInfo license, TimeSpan usageTime)
        {
            try
            {
                license.UsedHours += (int)Math.Ceiling(usageTime.TotalHours);
                return await Task.FromResult(true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SaveLicenseAsync(string devicePath, LicenseInfo license)
        {
            try
            {
                var licensePath = Path.Combine(devicePath, LicenseFileName);
                var json = JsonConvert.SerializeObject(license, Formatting.Indented);
                var data = Encoding.UTF8.GetBytes(json);
                var encryptedData = Encrypt(data);
                
                await File.WriteAllBytesAsync(licensePath, encryptedData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsLicenseExpired(LicenseInfo license)
        {
            return DateTime.Now > license.ExpirationDate;
        }

        public bool IsUsageLimitExceeded(LicenseInfo license)
        {
            return license.UsedHours >= license.MaxUsageHours && license.MaxUsageHours > 0;
        }

        public TimeSpan GetRemainingTime(LicenseInfo license)
        {
            if (license.MaxUsageHours <= 0)
                return TimeSpan.MaxValue;

            var remainingHours = Math.Max(0, license.MaxUsageHours - license.UsedHours);
            return TimeSpan.FromHours(remainingHours);
        }

        private byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = ResizeKey(_encryptionKey, 32);
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private byte[] Decrypt(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = ResizeKey(_encryptionKey, 32);
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        private byte[] ResizeKey(byte[] key, int size)
        {
            var resizedKey = new byte[size];
            Array.Copy(key, resizedKey, Math.Min(key.Length, size));
            return resizedKey;
        }
    }
}