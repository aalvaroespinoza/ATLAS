namespace ATLAS.Storage.Database;

/// <summary>
/// Configuration helper for database paths and connection strings.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Returns the default path for the SQLite database (%LocalAppData%\ATLAS\atlas.db).
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "ATLAS", "atlas.db");
    }

    /// <summary>
    /// Returns the default SQLite connection string pointing to %LocalAppData%\ATLAS\atlas.db.
    /// </summary>
    public static string GetDefaultConnectionString()
    {
        var dbPath = GetDefaultDatabasePath();
        return $"Data Source={dbPath}";
    }
}
