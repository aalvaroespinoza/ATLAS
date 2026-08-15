namespace ATLAS.UI.ViewModels;

/// <summary>
/// Presentation model for rendering a lightweight dual-bar or single-bar in the native WinUI chart.
/// </summary>
public class DailyFinanceBarItem
{
    public string DayLabel { get; init; } = string.Empty;
    public string FullDateLabel { get; init; } = string.Empty;
    public decimal ExpenseAmount { get; init; }
    public decimal IncomeAmount { get; init; }
    public double ExpenseHeightPx { get; init; } // 4 to 60 px
    public double IncomeHeightPx { get; init; } // 4 to 60 px
    public string ToolTipText { get; init; } = string.Empty;
    public bool HasMovement => ExpenseAmount > 0 || IncomeAmount > 0;
}
