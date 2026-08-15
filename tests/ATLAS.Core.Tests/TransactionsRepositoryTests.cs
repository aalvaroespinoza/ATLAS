using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class TransactionsRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;
    private readonly TransactionsRepository _repository;

    public TransactionsRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_tx_repo_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
        var initializer = new DatabaseInitializer(_connectionString);
        initializer.InitializeAsync().GetAwaiter().GetResult();
        _repository = new TransactionsRepository(_connectionString);
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
    public async Task CreateAsync_ShouldPersistTransactionCorrectly()
    {
        // Arrange
        var tx = new Transaction
        {
            Monto = 3500.50m,
            Tipo = "expense",
            Origen = "manual",
            Descripcion = "Supermercado Día",
            Categoria = "Comida",
            Moneda = "ARS"
        };

        // Act
        var created = await _repository.CreateAsync(tx);
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3500.50m, retrieved.Monto);
        Assert.Equal("Supermercado Día", retrieved.Descripcion);
        Assert.Equal("Comida", retrieved.Categoria);
        Assert.Equal("expense", retrieved.Tipo);
        Assert.Equal("manual", retrieved.Origen);
        Assert.Equal("ARS", retrieved.Moneda);
        Assert.Equal("approved", retrieved.Estado);
    }

    [Fact]
    public async Task GetByExternalIdAsync_ShouldFindTransactionByIdExterno()
    {
        // Arrange
        var tx = new Transaction
        {
            Monto = 1200m,
            Tipo = "expense",
            Origen = "mercadopago",
            Descripcion = "Cafetería",
            IdExterno = "MP_PAYMENT_998877"
        };
        await _repository.CreateAsync(tx);

        // Act
        var retrieved = await _repository.GetByExternalIdAsync("MP_PAYMENT_998877");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("MP_PAYMENT_998877", retrieved.IdExterno);
        Assert.Equal(1200m, retrieved.Monto);
    }

    [Fact]
    public async Task CreateBatchAsync_ShouldInsertTransactionsAndIgnoreDuplicatesByIdExterno()
    {
        // Arrange
        var list = new List<Transaction>
        {
            new() { Monto = 500m, Descripcion = "Viaje Uber", IdExterno = "MP_1001", Origen = "mercadopago" },
            new() { Monto = 750m, Descripcion = "Farmacia", IdExterno = "MP_1002", Origen = "mercadopago" }
        };

        // Act 1: First batch insertion
        var insertedCount1 = await _repository.CreateBatchAsync(list);

        // Act 2: Second batch with duplicate MP_1001 and new MP_1003
        var list2 = new List<Transaction>
        {
            new() { Monto = 500m, Descripcion = "Viaje Uber Duplicado", IdExterno = "MP_1001", Origen = "mercadopago" },
            new() { Monto = 900m, Descripcion = "Librería", IdExterno = "MP_1003", Origen = "mercadopago" }
        };
        var insertedCount2 = await _repository.CreateBatchAsync(list2);

        var all = await _repository.GetRecentAsync(10);

        // Assert
        Assert.Equal(2, insertedCount1);
        Assert.Equal(1, insertedCount2); // Only MP_1003 inserted
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnOrderedByDateDescending()
    {
        // Arrange
        var t1 = new Transaction { Fecha = DateTimeOffset.UtcNow.AddHours(-2), Monto = 100, Descripcion = "Primero" };
        var t2 = new Transaction { Fecha = DateTimeOffset.UtcNow.AddHours(-1), Monto = 200, Descripcion = "Segundo" };
        var t3 = new Transaction { Fecha = DateTimeOffset.UtcNow, Monto = 300, Descripcion = "Tercero" };

        await _repository.CreateAsync(t1);
        await _repository.CreateAsync(t2);
        await _repository.CreateAsync(t3);

        // Act
        var recent = await _repository.GetRecentAsync(2);

        // Assert
        Assert.Equal(2, recent.Count);
        Assert.Equal("Tercero", recent[0].Descripcion);
        Assert.Equal("Segundo", recent[1].Descripcion);
    }
}
