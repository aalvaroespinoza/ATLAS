namespace ATLAS.Core.Context;

/// <summary>
/// Universal context service contract for composing transversal operational state from repositories.
/// Answers the question: "What is relevant right now for the user?"
/// Agnostic of presentation layers, consumable by UI, AI, Launcher, and Integrations.
/// </summary>
public interface IAtlasContextService
{
    /// <summary>
    /// Generates the complete, high-fidelity daily context snapshot for the user.
    /// </summary>
    Task<AtlasContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a lightweight, essential context snapshot for quick surfaces like Launcher or widget ribbons.
    /// </summary>
    Task<AtlasReducedContext> GetReducedContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gathers domain context and related activity for a specific target entity (Note, Goal, Habit, Roadmap, Transaction).
    /// </summary>
    Task<AtlasEntityContext> GetEntityContextAsync(object entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gathers prioritized attention signals (pending habits, urgent milestones, alerts).
    /// </summary>
    Task<IReadOnlyList<AtlasAttentionSignal>> GetAttentionSignalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the next pending actionable milestone from active roadmaps.
    /// </summary>
    Task<AtlasRoadmapSignal?> GetNextMilestoneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the habits summary and daily completion stats.
    /// </summary>
    Task<AtlasHabitsSummary> GetHabitsSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current month's financial metrics and recent movements.
    /// </summary>
    Task<AtlasFinanceSummary> GetFinanceSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a concise, structured plain-text prompt summarizing current personal context for Gemini AI system instruction injection.
    /// </summary>
    Task<string> BuildAiSystemContextPromptAsync(CancellationToken cancellationToken = default);
}
