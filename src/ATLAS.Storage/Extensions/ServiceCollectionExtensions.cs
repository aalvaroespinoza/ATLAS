using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Storage.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ATLAS Storage services (SQLite database initializer and repositories) into DI.
    /// </summary>
    public static IServiceCollection AddAtlasStorage(this IServiceCollection services, string? connectionString = null)
    {
        var connStr = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
        services.AddSingleton(new DatabaseInitializer(connStr));
        services.AddSingleton<INoteRepository>(_ => new NotesRepository(connStr));
        return services;
    }
}
