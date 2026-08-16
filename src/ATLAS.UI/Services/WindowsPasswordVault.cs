using ATLAS.Core.Security;
using Windows.Security.Credentials;

namespace ATLAS.UI.Services;

/// <summary>
/// Implementation of ISecretVault using the Windows Credential Locker (PasswordVault).
/// </summary>
public sealed class WindowsPasswordVault : ISecretVault
{
    private const string ResourceName = "ATLAS.PersonalOS";
    private readonly PasswordVault _vault = new();

    public void SetSecret(string key, string secret)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        if (secret == null)
            throw new ArgumentNullException(nameof(secret));

        RemoveExisting(key);

        var credential = new PasswordCredential(ResourceName, key, secret);
        _vault.Add(credential);
    }

    public string? GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            var credential = _vault.Retrieve(ResourceName, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return null;
        }
    }

    public void DeleteSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        RemoveExisting(key);
    }

    public bool HasSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        try
        {
            _vault.Retrieve(ResourceName, key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RemoveExisting(string key)
    {
        try
        {
            var existing = _vault.Retrieve(ResourceName, key);
            _vault.Remove(existing);
        }
        catch
        {
            // Ignore if doesn't exist
        }
    }
}
