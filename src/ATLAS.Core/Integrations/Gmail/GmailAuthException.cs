namespace ATLAS.Core.Integrations.Gmail;

/// <summary>
/// Exception thrown when Gmail OAuth authorization fails, is revoked, or has expired.
/// </summary>
public class GmailAuthException : Exception
{
    public GmailAuthException(string message) : base(message) { }

    public GmailAuthException(string message, Exception innerException) : base(message, innerException) { }
}
