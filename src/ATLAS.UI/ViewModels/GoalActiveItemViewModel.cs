using ATLAS.Core.Entities;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

public sealed class GoalActiveItemViewModel
{
    public Goal Goal { get; }
    public string Title => Goal.Title;
    public int Progress => Math.Clamp(Goal.Progress, 0, 100);
    public string ProgressText => $"[{Progress}%]";
    public string ProgressBarAscii => GenerateAsciiBar(Progress);
    public string? TargetDateText { get; }
    public Visibility TargetDateVisibility => !string.IsNullOrWhiteSpace(TargetDateText) ? Visibility.Visible : Visibility.Collapsed;

    public GoalActiveItemViewModel(Goal goal)
    {
        Goal = goal ?? throw new ArgumentNullException(nameof(goal));
        TargetDateText = goal.TargetDate?.ToLocalTime().ToString("dd MMM yyyy");
    }

    private static string GenerateAsciiBar(int percentage)
    {
        const int totalBlocks = 10;
        var filledBlocks = (int)Math.Round((double)percentage / 100 * totalBlocks);
        return new string('=', filledBlocks) + new string('-', totalBlocks - filledBlocks);
    }
}
