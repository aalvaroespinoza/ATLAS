using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Gmail;

public class GmailSyncService : IGmailSyncService
{
    private readonly IGmailClient _gmailClient;
    private readonly IActivityRepository _activityRepository;
    private readonly ISecretVault _secretVault;

    public GmailSyncService(IGmailClient gmailClient, IActivityRepository activityRepository, ISecretVault secretVault)
    {
        _gmailClient = gmailClient ?? throw new ArgumentNullException(nameof(gmailClient));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public async Task<int> SyncRecentActivityAsync(CancellationToken cancellationToken = default)
    {
        if (!_secretVault.HasSecret(SecretKeys.GmailClientId) || !_secretVault.HasSecret(SecretKeys.GmailRefreshToken))
        {
            return 0; // Not configured
        }

        try
        {
            var recentMessages = await _gmailClient.ListRecentMessagesAsync(limit: 20, query: null, cancellationToken).ConfigureAwait(false);
            int ingestedCount = 0;

            foreach (var msg in recentMessages)
            {
                var sourceId = $"gmail_{msg.Id}";
                bool exists = await _activityRepository.ExistsBySourceIdAsync(sourceId, cancellationToken).ConfigureAwait(false);
                
                if (exists)
                    continue;

                var activity = MapToActivity(msg);
                if (activity != null)
                {
                    await _activityRepository.CreateAsync(activity, cancellationToken).ConfigureAwait(false);
                    ingestedCount++;
                }
            }

            return ingestedCount;
        }
        catch (Exception ex)
        {
            // Logically we could use an ILogger here, but we fail silently for background syncs to not crash the app
            Console.WriteLine($"[GmailSyncService] Error: {ex.Message}");
            return 0;
        }
    }

    private static ActivityRecord? MapToActivity(GmailMessageSummary msg)
    {
        var subjectLower = (msg.Subject ?? string.Empty).ToLowerInvariant();
        var fromLower = (msg.From ?? string.Empty).ToLowerInvariant();
        var snippetLower = (msg.Snippet ?? string.Empty).ToLowerInvariant();

        // 1. Steam Purchases / Activity
        if (fromLower.Contains("steampowered.com") || subjectLower.Contains("steam"))
        {
            return new ActivityRecord
            {
                Type = "gaming",
                SourceId = $"gmail_{msg.Id}",
                Title = "Actividad en Steam",
                Summary = !string.IsNullOrWhiteSpace(msg.Subject) ? msg.Subject : msg.Snippet,
                RelevanceScore = 5,
                Timestamp = msg.Date
            };
        }

        // 2. Hack4u / Education
        if (fromLower.Contains("hack4u") || subjectLower.Contains("hack4u"))
        {
            return new ActivityRecord
            {
                Type = "education",
                SourceId = $"gmail_{msg.Id}",
                Title = "Novedad en Hack4u",
                Summary = msg.Subject,
                RelevanceScore = 6,
                Timestamp = msg.Date
            };
        }

        // 3. Security Alerts
        if (subjectLower.Contains("security alert") || 
            subjectLower.Contains("alerta de seguridad") ||
            subjectLower.Contains("nuevo dispositivo") ||
            subjectLower.Contains("inicio de sesión") ||
            subjectLower.Contains("new sign-in"))
        {
            return new ActivityRecord
            {
                Type = "security",
                SourceId = $"gmail_{msg.Id}",
                Title = "Alerta de Seguridad",
                Summary = $"{msg.From}: {msg.Subject}",
                RelevanceScore = 9,
                Timestamp = msg.Date
            };
        }

        return null;
    }
}
