using System.Globalization;
using ATLAS.Core.Entities;

namespace ATLAS.UI.ViewModels;

public sealed class RecentActivityItemViewModel
{
    public string Type { get; }
    public string Title { get; }
    public string Detail { get; }
    public string TimestampText { get; }
    public string RelativeTime => TimestampText;

    public RecentActivityItemViewModel(Note note)
    {
        Type = "Nota";
        Title = !string.IsNullOrWhiteSpace(note.Title) ? note.Title : (note.Content.Length > 30 ? note.Content[..30] + "..." : note.Content);
        Detail = note.Content;
        TimestampText = FormatRelative(note.CreatedAt);
    }

    public RecentActivityItemViewModel(Habit habit, HabitEvent habitEvent)
    {
        Type = "Hábito";
        Title = habit.Name;
        Detail = "Hábito completado";
        TimestampText = FormatRelative(habitEvent.CompletedAt);
    }

    private static string FormatRelative(DateTimeOffset dto)
    {
        var dt = dto.ToLocalTime();
        var now = DateTimeOffset.Now;
        var diff = now - dt;

        if (diff.TotalMinutes < 1) return "hace instantes";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24 && dt.Date == now.Date) return $"hoy {dt:HH:mm}";
        if (diff.TotalDays < 2 && dt.Date == now.Date.AddDays(-1)) return $"ayer {dt:HH:mm}";

        return dt.ToString("dd MMM", CultureInfo.CurrentCulture);
    }
}
