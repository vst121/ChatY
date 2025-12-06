using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;

namespace ChatY.Infrastructure.Services;

public interface IAzureKeyVaultService
{
    Task<string> GetEncryptionKeyAsync(string keyName);
    Task<byte[]> EncryptAsync(string keyName, byte[] data);
    Task<byte[]> DecryptAsync(string keyName, byte[] encryptedData);
}

public class AzureKeyVaultService : IAzureKeyVaultService
{
    private readonly KeyClient _keyClient;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, CryptographyClient> _cryptoClients = new();

    public AzureKeyVaultService(IConfiguration configuration)
    {
        _configuration = configuration;
        var keyVaultUrl = configuration["Azure:KeyVault:Url"];
        _keyClient = new KeyClient(new Uri(keyVaultUrl!), new DefaultAzureCredential());
    }

    public async Task<string> GetEncryptionKeyAsync(string keyName)
    {
        var key = await _keyClient.GetKeyAsync(keyName);
        return key.Value.Id.ToString();
    }

    public async Task<byte[]> EncryptAsync(string keyName, byte[] data)
    {
        var cryptoClient = GetCryptoClient(keyName);
        var result = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, data);
        return result.Ciphertext;
    }

    public async Task<byte[]> DecryptAsync(string keyName, byte[] encryptedData)
    {
        var cryptoClient = GetCryptoClient(keyName);
        var result = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedData);
        return result.Plaintext;
    }

    private CryptographyClient GetCryptoClient(string keyName)
    {
        if (!_cryptoClients.TryGetValue(keyName, out var client))
        {
            var key = _keyClient.GetKey(keyName);
            client = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
            _cryptoClients[keyName] = client;
        }
        return client;
    }
}


