using ATLAS.Core.Security;
using Windows.Security.Credentials;

namespace ATLAS.UI.Services;

/// <summary>
/// Implements ISecretVault using Windows Credential Locker (Windows.Security.Credentials.PasswordVault).
/// </summary>
public class WindowsPasswordVault : ISecretVault
{
    private const string ResourceName = "ATLAS.PersonalOS";
    private readonly PasswordVault _vault = new();

    public void SetSecret(string key, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(secret);

        // Remove existing credential if already stored to avoid duplicates
        DeleteSecret(key);

        var credential = new PasswordCredential(ResourceName, key, secret);
        _vault.Add(credential);
    }

    public string? GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        try
        {
            var credential = _vault.Retrieve(ResourceName, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            // Windows throws exception when credential is not found in vault
            return null;
        }
    }

    public void DeleteSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        try
        {
            var credential = _vault.Retrieve(ResourceName, key);
            _vault.Remove(credential);
        }
        catch
        {
            // Not found
        }
    }

    public bool HasSecret(string key)
    {
        return !string.IsNullOrWhiteSpace(GetSecret(key));
    }
}
