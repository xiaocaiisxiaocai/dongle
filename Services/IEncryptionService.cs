using System;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public interface IEncryptionService
    {
        byte[] EncryptData(byte[] data, string key);
        byte[] DecryptData(byte[] encryptedData, string key);
        string EncryptString(string plainText, string key);
        string DecryptString(string encryptedText, string key);
        bool ValidateChecksum(byte[] data, string expectedChecksum);
        string GenerateChecksum(byte[] data);
        Task<bool> EncryptFileAsync(string inputPath, string outputPath, string key);
        Task<bool> DecryptFileAsync(string inputPath, string outputPath, string key);
    }
}