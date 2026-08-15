using System.Collections.ObjectModel;
using System.Globalization;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Finance;
using ATLAS.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.UI.ViewModels;

public partial class FinanceViewModel : ObservableObject
{
    private static readonly CultureInfo ArCulture = new("es-AR");
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICommandRegistry _commandRegistry;

    public ObservableCollection<TransactionItemViewModel> Transactions { get; } = [];
    public ObservableCollection<DailyFinanceBarItem> FinanceDailyBars { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTransactionsVisibility))]
    public partial bool HasNoTransactions { get; set; }

    public Visibility HasNoTransactionsVisibility => HasNoTransactions ? Visibility.Visible : Visibility.Collapsed;

    // Summary metrics
    [ObservableProperty]
    public partial string TotalExpensesFormatted { get; set; } = "$0";

    [ObservableProperty]
    public partial string TotalIncomeFormatted { get; set; } = "$0";

    [ObservableProperty]
    public partial string NetBalanceFormatted { get; set; } = "$0";

    // Quick Add Expense Form
    [ObservableProperty]
    public partial string NewExpenseAmountInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExpenseDescriptionInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewExpenseCategoryInput { get; set; } = string.Empty;

    // Sync State & Feedback
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSyncingVisibility))]
    public partial bool IsSyncing { get; set; }

    public Visibility IsSyncingVisibility => IsSyncing ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasStatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSeverity))]
    public partial bool IsSuccessStatus { get; set; }

    public InfoBarSeverity StatusSeverity => IsSuccessStatus ? InfoBarSeverity.Success : InfoBarSeverity.Error;

    public FinanceViewModel(ITransactionRepository transactionRepository, ICommandRegistry commandRegistry)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        try
        {
            var list = await _transactionRepository.GetRecentAsync(100);
            Transactions.Clear();

            decimal totalExpenses = 0;
            decimal totalIncome = 0;

            foreach (var t in list)
            {
                Transactions.Add(new TransactionItemViewModel(t));
                if (t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                {
                    totalIncome += t.Monto;
                }
                else
                {
                    totalExpenses += t.Monto;
                }
            }

            TotalExpensesFormatted = $"${totalExpenses.ToString("N0", ArCulture)}";
            TotalIncomeFormatted = $"${totalIncome.ToString("N0", ArCulture)}";

            var net = totalIncome - totalExpenses;
            NetBalanceFormatted = (net >= 0 ? "+ $" : "- $") + Math.Abs(net).ToString("N0", ArCulture);

            // Generate 14-day visual breakdown
            FinanceDailyBars.Clear();
            var now = DateTimeOffset.UtcNow.Date;
            var days = Enumerable.Range(0, 14)
                .Select(i => now.AddDays(-13 + i))
                .ToList();

            var dailyExpenses = list
                .Where(t => !t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Fecha.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

            var dailyIncomes = list
                .Where(t => t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Fecha.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

            var maxVal = Math.Max(
                dailyExpenses.Values.DefaultIfEmpty(0).Max(),
                dailyIncomes.Values.DefaultIfEmpty(0).Max());
            if (maxVal == 0) maxVal = 1;

            foreach (var day in days)
            {
                dailyExpenses.TryGetValue(day, out var exp);
                dailyIncomes.TryGetValue(day, out var inc);

                var expRatio = (double)(exp / maxVal);
                var incRatio = (double)(inc / maxVal);

                var expHeight = exp > 0 ? Math.Max(6, expRatio * 50) : 3;
                var incHeight = inc > 0 ? Math.Max(6, incRatio * 50) : 3;

                var tooltip = $"{day:dddd dd/MM}\nGastos: ${exp.ToString("N0", ArCulture)}\nIngresos: ${inc.ToString("N0", ArCulture)}";

                FinanceDailyBars.Add(new DailyFinanceBarItem
                {
                    DayLabel = day.ToString("dd/MM", ArCulture),
                    FullDateLabel = day.ToString("dd/MM"),
                    ExpenseAmount = exp,
                    IncomeAmount = inc,
                    ExpenseHeightPx = expHeight,
                    IncomeHeightPx = incHeight,
                    ToolTipText = tooltip
                });
            }

            HasNoTransactions = Transactions.Count == 0;
        }
        catch (Exception ex)
        {
            SetStatus($"Error al cargar finanzas: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public async Task AddExpenseAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExpenseAmountInput) || string.IsNullOrWhiteSpace(NewExpenseDescriptionInput))
        {
            SetStatus("Por favor ingresá un monto y una descripción para el gasto.", isSuccess: false);
            return;
        }

        if (!ExpenseTextParser.TryParseAmount(NewExpenseAmountInput, out var amount))
        {
            SetStatus("El monto ingresado no es un número válido.", isSuccess: false);
            return;
        }

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["amount"] = amount,
                ["description"] = NewExpenseDescriptionInput.Trim(),
                ["category"] = string.IsNullOrWhiteSpace(NewExpenseCategoryInput) ? null : NewExpenseCategoryInput.Trim(),
                ["origin"] = "desktop_app",
                ["type"] = "expense"
            };

            var result = await _commandRegistry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                NewExpenseAmountInput = string.Empty;
                NewExpenseDescriptionInput = string.Empty;
                NewExpenseCategoryInput = string.Empty;
                SetStatus("✓ Gasto registrado con éxito.", isSuccess: true);
                await LoadTransactionsAsync();
            }
            else
            {
                SetStatus(result.ErrorMessage ?? "Error al registrar gasto.", isSuccess: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public async Task SyncMercadoPagoAsync()
    {
        if (IsSyncing) return;

        IsSyncing = true;
        HasStatusMessage = false;

        try
        {
            var result = await _commandRegistry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId);
            if (result.IsSuccess)
            {
                SetStatus("✓ Sincronización con Mercado Pago completada.", isSuccess: true);
                await LoadTransactionsAsync();
            }
            else
            {
                SetStatus(result.ErrorMessage ?? "Error al sincronizar con Mercado Pago.", isSuccess: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", isSuccess: false);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        IsSuccessStatus = isSuccess;
        HasStatusMessage = true;
    }
}
