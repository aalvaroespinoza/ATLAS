using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Integrations.Telegram;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class TelegramMessageProcessorTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public TelegramMessageProcessorTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_tg_proc_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch
        {
            // Ignore cleanup
        }
        GC.SuppressFinalize(this);
    }

    private async Task<(TelegramMessageProcessor Processor, CommandRegistry Registry, NotesRepository NotesRepo, HabitsRepository HabitsRepo, TransactionsRepository TxRepo)> SetupEnvironmentAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var notesRepo = new NotesRepository(_connectionString);
        var habitsRepo = new HabitsRepository(_connectionString);
        var txRepo = new TransactionsRepository(_connectionString);

        var registry = new CommandRegistry();
        registry.Register(new CaptureNoteCommand(notesRepo));
        registry.Register(new HabitCompleteCommand(habitsRepo));
        registry.Register(new FinanceAddTransactionCommand(txRepo));

        var processor = new TelegramMessageProcessor(registry, habitsRepo);
        return (processor, registry, notesRepo, habitsRepo, txRepo);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenFreeText_ShouldExecuteCaptureNote_AndReturnConfirmation()
    {
        // Arrange
        var (processor, _, notesRepo, _, _) = await SetupEnvironmentAsync();
        var message = new TelegramMessage(
            UpdateId: 1,
            MessageId: 100,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "Recordar comprar repuesto de monitor",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Equal("✓ Nota guardada en ATLAS.", response);

        var notes = await notesRepo.GetRecentAsync(5);
        Assert.Single(notes);
        Assert.Equal("Recordar comprar repuesto de monitor", notes[0].Content);
        Assert.Equal("telegram", notes[0].Source);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHabitCommand_WithMatchingHabit_ShouldExecuteHabitComplete_AndReturnConfirmation()
    {
        // Arrange
        var (processor, _, _, habitsRepo, _) = await SetupEnvironmentAsync();
        var habit = await habitsRepo.CreateAsync(new Habit
        {
            Id = "h-water",
            Name = "Tomar 2L de agua",
            Frequency = "daily"
        });

        var message = new TelegramMessage(
            UpdateId: 2,
            MessageId: 101,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "/habit agua",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Equal("✓ Hábito 'Tomar 2L de agua' completado.", response);

        var events = await habitsRepo.GetEventsAsync(habit.Id);
        Assert.Single(events);
        Assert.Equal("vía Telegram", events[0].Note);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenDoneCommand_WithCustomNote_ShouldRecordCustomNote()
    {
        // Arrange
        var (processor, _, _, habitsRepo, _) = await SetupEnvironmentAsync();
        var habit = await habitsRepo.CreateAsync(new Habit
        {
            Id = "h-gym",
            Name = "Gimnasio",
            Frequency = "days:1,3,5"
        });

        var message = new TelegramMessage(
            UpdateId: 3,
            MessageId: 102,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "/done gim: Rutina de pecho y bíceps",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Equal("✓ Hábito 'Gimnasio' completado.", response);

        var events = await habitsRepo.GetEventsAsync(habit.Id);
        Assert.Single(events);
        Assert.Equal("Rutina de pecho y bíceps", events[0].Note);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenExpenseCommand_ShouldExecuteFinanceAddTransaction_AndReturnConfirmation()
    {
        // Arrange
        var (processor, _, _, _, txRepo) = await SetupEnvironmentAsync();
        var message = new TelegramMessage(
            UpdateId: 4,
            MessageId: 103,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "/expense 4500 Cena con amigos",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Contains("4.500", response);
        Assert.Contains("Cena con amigos", response);

        var transactions = await txRepo.GetRecentAsync(5);
        Assert.Single(transactions);
        Assert.Equal(4500m, transactions[0].Monto);
        Assert.Equal("Cena con amigos", transactions[0].Descripcion);
        Assert.Equal("telegram", transactions[0].Origen);
        Assert.Equal("expense", transactions[0].Tipo);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenGastoCommandWithSymbol_ShouldParseAndSaveCorrectly()
    {
        // Arrange
        var (processor, _, _, _, txRepo) = await SetupEnvironmentAsync();
        var message = new TelegramMessage(
            UpdateId: 5,
            MessageId: 104,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "/gasto $1.250 Farmacia",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Contains("1.250", response);
        Assert.Contains("Farmacia", response);

        var transactions = await txRepo.GetRecentAsync(5);
        Assert.Single(transactions);
        Assert.Equal(1250m, transactions[0].Monto);
        Assert.Equal("Farmacia", transactions[0].Descripcion);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHabitCommand_WithNonExistentHabit_ShouldReturnNotFoundError()
    {
        // Arrange
        var (processor, _, _, _, _) = await SetupEnvironmentAsync();
        var message = new TelegramMessage(
            UpdateId: 6,
            MessageId: 105,
            ChatId: 12345,
            FromUsername: "alvaro",
            Text: "/habit paracaidismo",
            Date: DateTimeOffset.UtcNow);

        // Act
        var response = await processor.ProcessMessageAsync(message);

        // Assert
        Assert.Equal("⚠️ No se encontró ningún hábito que coincida con 'paracaidismo'.", response);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenStartOrHelpCommand_ShouldReturnWelcomeMessage()
    {
        // Arrange
        var (processor, _, _, _, _) = await SetupEnvironmentAsync();
        var startMessage = new TelegramMessage(7, 106, 12345, "alvaro", "/start", DateTimeOffset.UtcNow);
        var helpMessage = new TelegramMessage(8, 107, 12345, "alvaro", "/help", DateTimeOffset.UtcNow);

        // Act
        var startResponse = await processor.ProcessMessageAsync(startMessage);
        var helpResponse = await processor.ProcessMessageAsync(helpMessage);

        // Assert
        Assert.Contains("Bot de ATLAS conectado", startResponse);
        Assert.Contains("capturar", startResponse, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/expense", startResponse);
        Assert.Equal(startResponse, helpResponse);
    }
}
