using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Supabase;

public class SupabaseSyncService : ISupabaseSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ISecretVault _secretVault;
    private readonly INoteRepository _noteRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly IHabitRepository _habitRepository;
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly ITransactionRepository _transactionRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SupabaseSyncService(
        HttpClient httpClient,
        ISecretVault secretVault,
        INoteRepository noteRepository,
        IGoalRepository goalRepository,
        IHabitRepository habitRepository,
        IRoadmapRepository roadmapRepository,
        ITransactionRepository transactionRepository)
    {
        _httpClient = httpClient;
        _secretVault = secretVault;
        _noteRepository = noteRepository;
        _goalRepository = goalRepository;
        _habitRepository = habitRepository;
        _roadmapRepository = roadmapRepository;
        _transactionRepository = transactionRepository;
    }

    public bool IsConfigured()
    {
        var url = _secretVault.GetSecret(SecretKeys.SupabaseUrl);
        var key = _secretVault.GetSecret(SecretKeys.SupabaseAnonKey);
        return !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(key);
    }

    public async Task<SupabaseSyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var url = _secretVault.GetSecret(SecretKeys.SupabaseUrl)?.TrimEnd('/');
        var key = _secretVault.GetSecret(SecretKeys.SupabaseAnonKey);

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            return new SupabaseSyncResult(false, "Supabase no está configurado (falta URL o Anon Key).");
        }

        try
        {
            // 1. Sync Notes
            var notes = (await _noteRepository.GetRecentAsync(500, cancellationToken)).ToList();
            if (notes.Count > 0)
            {
                var notesPayload = notes.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    content = n.Content,
                    type = n.Type,
                    tags = n.Tags,
                    source = n.Source,
                    created_at = n.CreatedAt.UtcDateTime.ToString("o")
                });
                await UpsertTableAsync(url, key, "notes", notesPayload, cancellationToken);
            }

            // 2. Sync Goals
            var goals = (await _goalRepository.GetAllAsync(null, cancellationToken)).ToList();
            if (goals.Count > 0)
            {
                var goalsPayload = goals.Select(g => new
                {
                    id = g.Id,
                    title = g.Title,
                    description = g.Description,
                    status = g.Status,
                    progress = g.Progress,
                    target_date = g.TargetDate?.UtcDateTime.ToString("o"),
                    created_at = g.CreatedAt.UtcDateTime.ToString("o")
                });
                await UpsertTableAsync(url, key, "goals", goalsPayload, cancellationToken);
            }

            // 3. Sync Habits
            var habits = (await _habitRepository.GetAllAsync(cancellationToken)).ToList();
            if (habits.Count > 0)
            {
                var habitsPayload = habits.Select(h => new
                {
                    id = h.Id,
                    name = h.Name,
                    description = h.Description,
                    frequency = h.Frequency,
                    created_at = h.CreatedAt.UtcDateTime.ToString("o")
                });
                await UpsertTableAsync(url, key, "habits", habitsPayload, cancellationToken);
            }

            // 4. Sync Habit Events
            var events = (await _habitRepository.GetEventsAsync(null, null, cancellationToken)).ToList();
            if (events.Count > 0)
            {
                var eventsPayload = events.Select(e => new
                {
                    id = e.Id,
                    habit_id = e.HabitId,
                    completed_at = e.CompletedAt.UtcDateTime.ToString("o"),
                    note = e.Note
                });
                await UpsertTableAsync(url, key, "habit_events", eventsPayload, cancellationToken);
            }

            // 5. Sync Roadmaps
            var roadmaps = (await _roadmapRepository.GetAllAsync(null, cancellationToken)).ToList();
            var milestonesCount = 0;
            if (roadmaps.Count > 0)
            {
                var roadmapsPayload = roadmaps.Select(r => new
                {
                    id = r.Id,
                    goal_id = r.GoalId,
                    title = r.Title,
                    description = r.Description,
                    status = r.Status,
                    created_at = r.CreatedAt.UtcDateTime.ToString("o"),
                    updated_at = r.UpdatedAt.UtcDateTime.ToString("o")
                });
                await UpsertTableAsync(url, key, "roadmaps", roadmapsPayload, cancellationToken);

                // 6. Sync Roadmap Milestones
                var allMilestones = roadmaps.SelectMany(r => r.Milestones ?? new List<RoadmapMilestone>()).ToList();
                milestonesCount = allMilestones.Count;
                if (allMilestones.Count > 0)
                {
                    var milestonesPayload = allMilestones.Select(m => new
                    {
                        id = m.Id,
                        roadmap_id = m.RoadmapId,
                        title = m.Title,
                        order_index = m.OrderIndex,
                        status = m.Status,
                        notes = m.Notes,
                        created_at = m.CreatedAt.UtcDateTime.ToString("o"),
                        completed_at = m.CompletedAt?.UtcDateTime.ToString("o")
                    });
                    await UpsertTableAsync(url, key, "roadmap_milestones", milestonesPayload, cancellationToken);
                }
            }

            // 7. Sync Transactions
            var txs = (await _transactionRepository.GetRecentAsync(500, cancellationToken)).ToList();
            if (txs.Count > 0)
            {
                var txPayload = txs.Select(t => new
                {
                    id = t.Id,
                    fecha = t.Fecha.UtcDateTime.ToString("o"),
                    monto = t.Monto,
                    tipo = t.Tipo,
                    origen = t.Origen,
                    descripcion = t.Descripcion,
                    moneda = t.Moneda,
                    categoria = t.Categoria,
                    subcategoria = t.Subcategoria,
                    id_externo = t.IdExterno,
                    estado = t.Estado,
                    metadata = t.Metadata,
                    created_at = t.CreatedAt.UtcDateTime.ToString("o")
                });
                await UpsertTableAsync(url, key, "transactions", txPayload, cancellationToken);
            }

            return new SupabaseSyncResult(
                IsSuccess: true,
                Message: "Sincronización con Supabase completada exitosamente.",
                NotesSynced: notes.Count,
                GoalsSynced: goals.Count,
                HabitsSynced: habits.Count,
                HabitEventsSynced: events.Count,
                RoadmapsSynced: roadmaps.Count,
                MilestonesSynced: milestonesCount,
                TransactionsSynced: txs.Count
            );
        }
        catch (Exception ex)
        {
            return new SupabaseSyncResult(false, $"Error durante la sincronización: {ex.Message}");
        }
    }

    private async Task UpsertTableAsync<T>(string baseUrl, string apiKey, string table, IEnumerable<T> records, CancellationToken cancellationToken)
    {
        var endpoint = $"{baseUrl}/rest/v1/{table}";
        var json = JsonSerializer.Serialize(records, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("apikey", apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("Prefer", "resolution=merge-duplicates");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error al sincronizar '{table}' (HTTP {response.StatusCode}): {errorBody}");
        }
    }
}
