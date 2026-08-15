using ATLAS.Core.Security;

namespace ATLAS.Core.Tests;

public class SecretKeysTests
{
    [Fact]
    public void SecretKeys_Constants_ShouldPreserveExactLegacyValues()
    {
        // Assert: GeminiApiKey must preserve exact string to avoid losing already configured user credentials
        Assert.Equal("GeminiApiKey", SecretKeys.GeminiApiKey);
        Assert.Equal("TelegramBotToken", SecretKeys.TelegramBotToken);
        Assert.Equal("MercadoPagoAccessToken", SecretKeys.MercadoPagoAccessToken);
    }
}
