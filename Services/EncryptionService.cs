using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseProtection.Services
{
    public class EncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int IvSize = 16;

        public byte[] EncryptData(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            
            var keyBytes = GenerateKey(key);
            var iv = GenerateRandomIV();
            
            aes.Key = keyBytes;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
            
            var result = new byte[iv.Length + encryptedData.Length];
            Array.Copy(iv, 0, result, 0, iv.Length);
            Array.Copy(encryptedData, 0, result, iv.Length, encryptedData.Length);
            
            return result;
        }

        public byte[] DecryptData(byte[] encryptedData, string key)
        {
            if (encryptedData.Length < IvSize)
                throw new ArgumentException("Invalid encrypted data length");

            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            
            var keyBytes = GenerateKey(key);
            var iv = new byte[IvSize];
            var cipherText = new byte[encryptedData.Length - IvSize];
            
            Array.Copy(encryptedData, 0, iv, 0, IvSize);
            Array.Copy(encryptedData, IvSize, cipherText, 0, cipherText.Length);
            
            aes.Key = keyBytes;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }

        public string EncryptString(string plainText, string key)
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = EncryptData(data, key);
            return Convert.ToBase64String(encryptedData);
        }

        public string DecryptString(string encryptedText, string key)
        {
            var encryptedData = Convert.FromBase64String(encryptedText);
            var decryptedData = DecryptData(encryptedData, key);
            return Encoding.UTF8.GetString(decryptedData);
        }

        public bool ValidateChecksum(byte[] data, string expectedChecksum)
        {
            var actualChecksum = GenerateChecksum(data);
            return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        public string GenerateChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash);
        }

        public async Task<bool> EncryptFileAsync(string inputPath, string outputPath, string key)
        {
            try
            {
                var data = await File.ReadAllBytesAsync(inputPath);
                var encryptedData = EncryptData(data, key);
                await File.WriteAllBytesAsync(outputPath, encryptedData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DecryptFileAsync(string inputPath, string outputPath, string key)
        {
            try
            {
                var encryptedData = await File.ReadAllBytesAsync(inputPath);
                var data = DecryptData(encryptedData, key);
                await File.WriteAllBytesAsync(outputPath, data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private byte[] GenerateKey(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("LicenseProtectionSalt2024"), 10000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        private byte[] GenerateRandomIV()
        {
            var iv = new byte[IvSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(iv);
            return iv;
        }
    }
}