namespace ATLAS.Core.Entities;

/// <summary>
/// Read-only presentation summary of an email message retrieved from Gmail.
/// </summary>
public record GmailMessageSummary(
    string Id,
    string ThreadId,
    string From,
    string Subject,
    string Snippet,
    DateTimeOffset Date
);
