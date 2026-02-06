using System.Security.Cryptography;
using System.Text;

namespace MedView.Server.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data like Study UIDs
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
    string EncryptStudyUid(string studyInstanceUid);
    string DecryptStudyUid(string encryptedStudyUid);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        
        // Get encryption key from configuration or generate a default one
        var keyString = configuration["Encryption:Key"] ?? "MedViewDefaultEncryptionKey32B";
        var ivString = configuration["Encryption:IV"] ?? "MedViewDefaultIV";
        
        // Ensure key is 32 bytes for AES-256
        _key = PadOrTruncate(Encoding.UTF8.GetBytes(keyString), 32);
        // Ensure IV is 16 bytes
        _iv = PadOrTruncate(Encoding.UTF8.GetBytes(ivString), 16);
        
        _logger.LogInformation("EncryptionService initialized");
    }

    private static byte[] PadOrTruncate(byte[] data, int length)
    {
        if (data.Length == length)
            return data;
        
        var result = new byte[length];
        Array.Copy(data, result, Math.Min(data.Length, length));
        return result;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var encrypted = msEncrypt.ToArray();
            return Convert.ToBase64String(encrypted)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", ""); // URL-safe base64
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting text");
            throw;
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentNullException(nameof(encryptedText));

        try
        {
            // Convert from URL-safe base64
            var base64 = encryptedText.Replace('-', '+').Replace('_', '/');
            
            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var buffer = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(buffer);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting text");
            throw;
        }
    }

    public string EncryptStudyUid(string studyInstanceUid)
    {
        if (string.IsNullOrEmpty(studyInstanceUid))
            throw new ArgumentNullException(nameof(studyInstanceUid));

        return Encrypt(studyInstanceUid);
    }

    public string DecryptStudyUid(string encryptedStudyUid)
    {
        if (string.IsNullOrEmpty(encryptedStudyUid))
            throw new ArgumentNullException(nameof(encryptedStudyUid));

        return Decrypt(encryptedStudyUid);
    }
}
