using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Entities;
using ATLAS.Core.Events;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Gmail;

public class GmailSyncService : IGmailSyncService
{
    private readonly IGmailClient _gmailClient;
    private readonly IActivityRepository _activityRepository;
    private readonly IAtlasEventBus _eventBus;
    private readonly ISecretVault _secretVault;

    public GmailSyncService(IGmailClient gmailClient, IActivityRepository activityRepository, IAtlasEventBus eventBus, ISecretVault secretVault)
    {
        _gmailClient = gmailClient ?? throw new ArgumentNullException(nameof(gmailClient));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
                    var domainEvent = new ATLAS.Core.Events.IntegrationActivityDetectedEvent(
                        IntegrationId: "gmail",
                        SourceId: activity.SourceId ?? $"gmail_{msg.Id}",
                        ActivityType: activity.Type,
                        Title: activity.Title,
                        Summary: activity.Summary ?? string.Empty,
                        RelevanceScore: activity.RelevanceScore,
                        Source: "gmail",
                        EventId: Guid.NewGuid().ToString("N"),
                        OccurredAt: activity.Timestamp
                    );
                    
                    await _eventBus.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
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

        // 1. Gaming Purchases / Activity
        if (fromLower.Contains("steampowered.com") || subjectLower.Contains("steam") || fromLower.Contains("roblox.com") || subjectLower.Contains("roblox"))
        {
            return new ActivityRecord
            {
                Type = "gaming",
                SourceId = $"gmail_{msg.Id}",
                Title = "Actividad en Juegos",
                Summary = !string.IsNullOrWhiteSpace(msg.Subject) ? msg.Subject : msg.Snippet,
                RelevanceScore = 5,
                Timestamp = msg.Date
            };
        }

        // 2. Education / Subscriptions
        if (fromLower.Contains("hack4u") || subjectLower.Contains("hack4u"))
        {
            return new ActivityRecord
            {
                Type = "education",
                SourceId = $"gmail_{msg.Id}",
                Title = "Novedad Educativa",
                Summary = msg.Subject,
                RelevanceScore = 6,
                Timestamp = msg.Date
            };
        }

        // 3. Security Alerts / Logins
        if (subjectLower.Contains("security alert") || 
            subjectLower.Contains("alerta de seguridad") ||
            subjectLower.Contains("nuevo dispositivo") ||
            subjectLower.Contains("inicio de sesión") ||
            subjectLower.Contains("new sign-in") ||
            fromLower.Contains("firefox") ||
            fromLower.Contains("mercadolibre.com") ||
            fromLower.Contains("binance.com") ||
            fromLower.Contains("openai.com"))
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
