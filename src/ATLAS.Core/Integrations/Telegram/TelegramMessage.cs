namespace ATLAS.Core.Integrations.Telegram;

/// <summary>
/// Domain model for an incoming message update from the Telegram Bot API.
/// </summary>
public record TelegramMessage(
    long UpdateId,
    long MessageId,
    long ChatId,
    string? FromUsername,
    string Text,
    DateTimeOffset Date);
