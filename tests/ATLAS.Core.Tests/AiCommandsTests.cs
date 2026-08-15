using ATLAS.Core.Ai;
using ATLAS.Core.Commands;

namespace ATLAS.Core.Tests;

public class AiCommandsTests
{
    private class MockAiProvider : IAiProvider
    {
        public Func<string, string>? SummarizeHandler { get; set; }
        public Func<string, string>? AskHandler { get; set; }

        public Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
        {
            if (SummarizeHandler != null)
            {
                return Task.FromResult(SummarizeHandler(text));
            }
            return Task.FromResult($"Resumen de: {text}");
        }

        public Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (AskHandler != null)
            {
                return Task.FromResult(AskHandler(prompt));
            }
            return Task.FromResult($"Respuesta a: {prompt}");
        }
    }

    [Fact]
    public async Task AiSummarizeCommand_ShouldReturnSummary_WhenValidTextProvided()
    {
        // Arrange
        var mockAi = new MockAiProvider
        {
            SummarizeHandler = input => $"Resumen conciso: {input.Length} caracteres procesados."
        };
        var registry = new CommandRegistry();
        registry.Register(new AiSummarizeCommand(mockAi));

        const string inputText = "Este es un texto largo sobre la arquitectura de ATLAS OS.";
        var parameters = new Dictionary<string, object?>
        {
            ["text"] = inputText
        };

        // Act
        var result = await registry.ExecuteAsync(AiSummarizeCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal($"Resumen conciso: {inputText.Length} caracteres procesados.", result.Data);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AiSummarizeCommand_ShouldFail_WhenTextIsMissingOrEmpty(string? emptyText)
    {
        // Arrange
        var mockAi = new MockAiProvider();
        var registry = new CommandRegistry();
        registry.Register(new AiSummarizeCommand(mockAi));

        var parameters = emptyText != null
            ? new Dictionary<string, object?> { ["text"] = emptyText }
            : new Dictionary<string, object?>();

        // Act
        var result = await registry.ExecuteAsync(AiSummarizeCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("El parámetro 'text' es obligatorio", result.ErrorMessage);
    }

    [Fact]
    public async Task AiSummarizeCommand_ShouldReturnFailure_WhenAiProviderThrows()
    {
        // Arrange
        var mockAi = new MockAiProvider
        {
            SummarizeHandler = _ => throw new InvalidOperationException("API key de Gemini no configurada.")
        };
        var registry = new CommandRegistry();
        registry.Register(new AiSummarizeCommand(mockAi));

        var parameters = new Dictionary<string, object?>
        {
            ["text"] = "Texto válido"
        };

        // Act
        var result = await registry.ExecuteAsync(AiSummarizeCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("API key de Gemini no configurada.", result.ErrorMessage);
    }

    [Fact]
    public async Task AiAskCommand_ShouldReturnAnswer_WhenValidPromptProvided()
    {
        // Arrange
        var mockAi = new MockAiProvider
        {
            AskHandler = prompt => $"Respuesta inteligente para '{prompt}'"
        };
        var registry = new CommandRegistry();
        registry.Register(new AiAskCommand(mockAi));

        var parameters = new Dictionary<string, object?>
        {
            ["prompt"] = "¿Cómo declaro variables en C#?"
        };

        // Act
        var result = await registry.ExecuteAsync(AiAskCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Respuesta inteligente para '¿Cómo declaro variables en C#?'", result.Data);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AiAskCommand_ShouldFail_WhenPromptIsMissingOrEmpty(string? emptyPrompt)
    {
        // Arrange
        var mockAi = new MockAiProvider();
        var registry = new CommandRegistry();
        registry.Register(new AiAskCommand(mockAi));

        var parameters = emptyPrompt != null
            ? new Dictionary<string, object?> { ["prompt"] = emptyPrompt }
            : new Dictionary<string, object?>();

        // Act
        var result = await registry.ExecuteAsync(AiAskCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("El parámetro 'prompt' es obligatorio", result.ErrorMessage);
    }

    [Fact]
    public async Task AiAskCommand_ShouldReturnFailure_WhenAiProviderThrows()
    {
        // Arrange
        var mockAi = new MockAiProvider
        {
            AskHandler = _ => throw new HttpRequestException("Error 503 Servicio no disponible.")
        };
        var registry = new CommandRegistry();
        registry.Register(new AiAskCommand(mockAi));

        var parameters = new Dictionary<string, object?>
        {
            ["prompt"] = "Pregunta válida"
        };

        // Act
        var result = await registry.ExecuteAsync(AiAskCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("Error 503 Servicio no disponible.", result.ErrorMessage);
    }

    [Fact]
    public void AiCommands_Metadata_ShouldBeCorrect()
    {
        // Arrange
        var mockAi = new MockAiProvider();
        var summarizeCmd = new AiSummarizeCommand(mockAi);
        var askCmd = new AiAskCommand(mockAi);

        // Assert summarize
        Assert.Equal("ai.summarize", summarizeCmd.Id);
        Assert.Equal("Resumir con IA", summarizeCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(summarizeCmd.Description));
        Assert.Contains(summarizeCmd.InputSchema, p => p.Name == "text" && p.IsRequired);

        // Assert ask
        Assert.Equal("ai.ask", askCmd.Id);
        Assert.Equal("Preguntar a la IA", askCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(askCmd.Description));
        Assert.Contains(askCmd.InputSchema, p => p.Name == "prompt" && p.IsRequired);
    }
}
