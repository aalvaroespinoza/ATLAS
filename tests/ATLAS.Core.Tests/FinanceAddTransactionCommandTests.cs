using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class FinanceAddTransactionCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;
    private readonly TransactionsRepository _repository;
    private readonly CommandRegistry _registry;

    public FinanceAddTransactionCommandTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_fin_cmd_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
        var initializer = new DatabaseInitializer(_connectionString);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _repository = new TransactionsRepository(_connectionString);
        _registry = new CommandRegistry();
        _registry.Register(new FinanceAddTransactionCommand(_repository));
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

    [Fact]
    public async Task ExecuteAsync_WithValidParameters_ShouldCreateTransactionWithDefaultManualOrigin()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = 4500.75m,
            ["description"] = "Cena de cumpleaños",
            ["category"] = "Salidas"
        };

        // Act
        var result = await _registry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters);

        // Assert
        Assert.True(result.IsSuccess);
        var tx = Assert.IsType<Transaction>(result.Data);
        Assert.Equal(4500.75m, tx.Monto);
        Assert.Equal("Cena de cumpleaños", tx.Descripcion);
        Assert.Equal("Salidas", tx.Categoria);
        Assert.Equal("expense", tx.Tipo);
        Assert.Equal("manual", tx.Origen);
        Assert.Equal("ARS", tx.Moneda);

        var persisted = await _repository.GetByIdAsync(tx.Id);
        Assert.NotNull(persisted);
        Assert.Equal(4500.75m, persisted.Monto);
    }

    [Fact]
    public async Task ExecuteAsync_WithNumericTypeVariants_ShouldParseAmountCorrectly()
    {
        // Arrange (passing double or int)
        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = 1500,
            ["description"] = "Carga SUBE",
            ["origin"] = "launcher"
        };

        // Act
        var result = await _registry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters);

        // Assert
        Assert.True(result.IsSuccess);
        var tx = Assert.IsType<Transaction>(result.Data);
        Assert.Equal(1500m, tx.Monto);
        Assert.Equal("launcher", tx.Origen);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAmountIsZeroOrNegative_ShouldReturnError()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = 0m,
            ["description"] = "Gasto inválido"
        };

        // Act
        var result = await _registry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("positivo mayor a 0", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDescriptionIsEmpty_ShouldReturnError()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = 1000m,
            ["description"] = "   "
        };

        // Act
        var result = await _registry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("description", result.ErrorMessage);
    }
}
