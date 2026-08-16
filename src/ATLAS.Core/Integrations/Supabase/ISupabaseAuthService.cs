using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Core.Integrations.Supabase;

public record SupabaseAuthResult(
    bool IsSuccess,
    string Message,
    string? UserId = null,
    string? Email = null
);

/// <summary>
/// Handles Supabase Auth session management (sign-in with password, token refresh, and persistence).
/// </summary>
public interface ISupabaseAuthService
{
    bool IsAuthenticated();
    string? GetUserId();
    string? GetUserEmail();
    Task<SupabaseAuthResult> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> RefreshSessionAsync(CancellationToken cancellationToken = default);
    void SignOut();
}
