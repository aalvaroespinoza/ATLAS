using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Ai;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using Xunit;

namespace ATLAS.Core.Tests;

public class FinanceCategorizeCommandTests
{
    private class FakeAiProvider : IAiProvider
    {
        public string ResponseToReturn { get; set; } = "Comida";
        public Exception? ExceptionToThrow { get; set; }
        public string? CapturedPrompt { get; private set; }

        public Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
        {
            CapturedPrompt = prompt;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(ResponseToReturn);
        }

        public Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResponseToReturn);
        }
    }

    private class FakeTransactionRepository : ITransactionRepository
    {
        private readonly Dictionary<string, Transaction> _transactions = new();

        public void Add(Transaction transaction) => _transactions[transaction.Id] = transaction;

        public Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            _transactions[transaction.Id] = transaction;
            return Task.FromResult(transaction);
        }

        public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            _transactions.TryGetValue(id, out var tx);
            return Task.FromResult(tx);
        }

        public Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Transaction?>(null);
        }

        public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(new List<Transaction>(_transactions.Values));
        }

        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default)
        {
            if (_transactions.TryGetValue(id, out var tx))
            {
                var updated = new Transaction
                {
                    Id = tx.Id,
                    Fecha = tx.Fecha,
                    Monto = tx.Monto,
                    Tipo = tx.Tipo,
                    Origen = tx.Origen,
                    Descripcion = tx.Descripcion,
                    Moneda = tx.Moneda,
                    Categoria = categoria,
                    Subcategoria = subcategoria,
                    IdExterno = tx.IdExterno,
                    Estado = tx.Estado,
                    Metadata = tx.Metadata,
                    CreatedAt = tx.CreatedAt
                };
                _transactions[id] = updated;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionIdMissing_ReturnsFailure()
    {
        var repo = new FakeTransactionRepository();
        var ai = new FakeAiProvider();
        var command = new FinanceCategorizeCommand(repo, ai);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>());

        Assert.False(result.IsSuccess);
        Assert.Contains("transaction_id", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionNotFound_ReturnsFailure()
    {
        var repo = new FakeTransactionRepository();
        var ai = new FakeAiProvider();
        var command = new FinanceCategorizeCommand(repo, ai);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["transaction_id"] = "non-existent-id"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("No se encontró la transacción", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDescriptionEmpty_ReturnsFailure()
    {
        var repo = new FakeTransactionRepository();
        repo.Add(new Transaction { Id = "tx-1", Descripcion = "   ", Monto = 100 });
        var ai = new FakeAiProvider();
        var command = new FinanceCategorizeCommand(repo, ai);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["transaction_id"] = "tx-1"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("no contiene una descripción", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ReturnsCleanedCategorySuggestionWithoutOverwriting()
    {
        var repo = new FakeTransactionRepository();
        repo.Add(new Transaction
        {
            Id = "tx-1",
            Descripcion = "Supermercado Coto compra semanal",
            Monto = 15000,
            Categoria = "OriginalManualCategory"
        });

        var ai = new FakeAiProvider
        {
            ResponseToReturn = "**Supermercado**\nExplicación adicional que debe ser ignorada."
        };

        var command = new FinanceCategorizeCommand(repo, ai);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["transaction_id"] = "tx-1"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Supermercado", result.Data);

        // Verify that the repository was NOT overwritten automatically
        var txAfter = await repo.GetByIdAsync("tx-1");
        Assert.Equal("OriginalManualCategory", txAfter?.Categoria);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiThrowsException_ReturnsFailure()
    {
        var repo = new FakeTransactionRepository();
        repo.Add(new Transaction { Id = "tx-1", Descripcion = "Pago de luz Edenor", Monto = 5000 });

        var ai = new FakeAiProvider
        {
            ExceptionToThrow = new InvalidOperationException("API quota exceeded")
        };

        var command = new FinanceCategorizeCommand(repo, ai);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["transaction_id"] = "tx-1"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("API quota exceeded", result.ErrorMessage);
    }
}
