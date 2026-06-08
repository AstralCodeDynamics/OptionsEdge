using System.Security.Cryptography;
using System.Text;

namespace OptionsEdge.API.Infrastructure.Security;

// AES-256-CBC encryption for at-rest secrets (e.g. Groww credentials). The key comes from
// "Encryption:Key" and is normalized to exactly 32 bytes (AES-256). Each ciphertext is
// stored as base64(IV || encrypted bytes) — a fresh random IV is generated per encryption.
public class AesEncryptionService(IConfiguration config) : IEncryptionService
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        (config["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key not configured"))
            .PadRight(32)[..32]);

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var result = new byte[aes.IV.Length + cipher.Length];
        aes.IV.CopyTo(result, 0);
        cipher.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = data[..16];

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
        return Encoding.UTF8.GetString(plain);
    }
}
