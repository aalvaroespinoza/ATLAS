namespace ATLAS.Core.Security;

/// <summary>
/// Abstraction for secure secret storage (e.g. Windows Credential Locker / PasswordVault).
/// </summary>
public interface ISecretVault
{
    /// <summary>
    /// Stores or updates a secret securely under the given key.
    /// </summary>
    void SetSecret(string key, string secret);

    /// <summary>
    /// Retrieves a secret by its key, or null if not found.
    /// </summary>
    string? GetSecret(string key);

    /// <summary>
    /// Deletes a secret from secure storage.
    /// </summary>
    void DeleteSecret(string key);

    /// <summary>
    /// Checks whether a secret exists for the given key.
    /// </summary>
    bool HasSecret(string key);
}
