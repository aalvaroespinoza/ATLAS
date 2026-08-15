using ATLAS.Core.Entities;

namespace ATLAS.UI.ViewModels;

public sealed class HabitTodayItemViewModel
{
    public Habit Habit { get; }
    public bool IsCompleted { get; }
    public string StatusSymbol => IsCompleted ? "[✓]" : "[ ]";
    public string Name => Habit.Name;
    public string FrequencyText => Habit.Frequency;
    public string CompletionDetail { get; }

    public HabitTodayItemViewModel(Habit habit, HabitEvent? latestEventToday)
    {
        Habit = habit ?? throw new ArgumentNullException(nameof(habit));
        IsCompleted = latestEventToday != null;

        if (latestEventToday != null)
        {
            var localTime = latestEventToday.CompletedAt.ToLocalTime();
            var noteSuffix = !string.IsNullOrWhiteSpace(latestEventToday.Note) ? $" — {latestEventToday.Note.Trim()}" : "";
            CompletionDetail = $"Completado a las {localTime:HH:mm}{noteSuffix}";
        }
        else
        {
            CompletionDetail = "Pendiente para hoy";
        }
    }
}
