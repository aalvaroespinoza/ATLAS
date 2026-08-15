using System.Globalization;
using ATLAS.Core.Entities;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

/// <summary>
/// Presentation model for displaying items (Notes or Habit actions) in the Launcher search list.
/// </summary>
public sealed class LauncherItemViewModel
{
    public Note? Note { get; }
    public Habit? Habit { get; }

    public string DisplayTitle { get; }
    public string? ContentSnippet { get; }
    public string? Tags { get; }
    public string Type { get; }
    public string FormattedDate { get; }
    public bool IsHabit => Habit != null;

    public Visibility ContentSnippetVisibility => !string.IsNullOrWhiteSpace(ContentSnippet) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TagsVisibility => !string.IsNullOrWhiteSpace(Tags) ? Visibility.Visible : Visibility.Collapsed;

    public LauncherItemViewModel(Note note)
    {
        Note = note ?? throw new ArgumentNullException(nameof(note));
        Habit = null;

        var hasExplicitTitle = !string.IsNullOrWhiteSpace(note.Title);
        DisplayTitle = hasExplicitTitle ? note.Title!.Trim() : ExtractFirstLine(note.Content);
        ContentSnippet = hasExplicitTitle ? note.Content?.Trim() : null;
        Tags = string.IsNullOrWhiteSpace(note.Tags) ? null : note.Tags.Trim();
        Type = string.IsNullOrWhiteSpace(note.Type) ? "note" : note.Type.Trim();
        FormattedDate = FormatRelativeDate(note.CreatedAt);
    }

    public LauncherItemViewModel(Habit habit)
    {
        Habit = habit ?? throw new ArgumentNullException(nameof(habit));
        Note = null;

        DisplayTitle = $"✓ Completar: {habit.Name.Trim()}";
        ContentSnippet = !string.IsNullOrWhiteSpace(habit.Description)
            ? habit.Description.Trim()
            : "Presioná Enter para registrar cumplimiento";
        Tags = null;
        Type = $"hábito • {habit.Frequency}";
        FormattedDate = "Hoy";
    }

    private static string ExtractFirstLine(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Nota sin contenido";
        }

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault()?.Trim();
        return !string.IsNullOrWhiteSpace(firstLine) ? firstLine : "Nota sin título";
    }

    private static string FormatRelativeDate(DateTimeOffset dateTimeOffset)
    {
        var dt = dateTimeOffset.ToLocalTime();
        var now = DateTimeOffset.Now;
        var diff = now - dt;

        if (diff.TotalMinutes < 1) return "ahora";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24 && dt.Date == now.Date) return $"hoy {dt:HH:mm}";
        if (diff.TotalDays < 2 && dt.Date == now.Date.AddDays(-1)) return $"ayer {dt:HH:mm}";

        return dt.ToString("dd MMM", CultureInfo.CurrentCulture);
    }
}
