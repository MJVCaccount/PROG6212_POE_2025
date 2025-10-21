using System.Security.Cryptography;
using System.Text;

namespace Contract_Monthly_Claim_System.Services
{
    public class EncryptionService
    {
        private readonly byte[] _key = Encoding.UTF8.GetBytes("16ByteSecretKey!"); // 128-bit key; in prod, use secure key
        private readonly byte[] _iv = Encoding.UTF8.GetBytes("16ByteInitVector"); // 128-bit IV

        public async Task<byte[]> EncryptAsync(Stream input)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await input.CopyToAsync(cryptoStream);
            cryptoStream.FlushFinalBlock();
            return memoryStream.ToArray();
        }

        public async Task<byte[]> DecryptAsync(byte[] encrypted)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var memoryStream = new MemoryStream(encrypted);
            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var output = new MemoryStream();
            await cryptoStream.CopyToAsync(output);
            return output.ToArray();
        }
    }
}