namespace JTDev.DbMigrator.Configuration;

/// <summary>
/// Configuration options for database migration operations.
/// Supports hierarchical configuration loading with priority: CLI args > Environment Variables > appsettings.json
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// PostgreSQL connection string.
    /// Priority: --connection-string (CLI) > DB_CONNECTION_STRING (ENV) > ConnectionStrings:DefaultConnection (appsettings)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Root path to migration scripts directory.
    /// Must be configured via appsettings.json (Migration:ScriptsPath) or CLI (--connection-string).
    /// </summary>
    public string ScriptsPath { get; set; } = "";

    /// <summary>
    /// Subdirectory containing schema DDL scripts.
    /// Default: "schema"
    /// </summary>
    public string SchemaPath { get; set; } = "schema";

    /// <summary>
    /// Subdirectory containing seed data scripts.
    /// Default: "seeds"
    /// </summary>
    public string SeedsPath { get; set; } = "seeds";

    /// <summary>
    /// Subdirectory containing migration scripts.
    /// Default: "migrations"
    /// </summary>
    public string MigrationsPath { get; set; } = "migrations";

    /// <summary>
    /// Timeout in seconds for database operations.
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets the full path to schema scripts.
    /// </summary>
    public string GetSchemaFullPath() => Path.Combine(ScriptsPath, SchemaPath);

    /// <summary>
    /// Gets the full path to seed data scripts.
    /// </summary>
    public string GetSeedsFullPath() => Path.Combine(ScriptsPath, SeedsPath);

    /// <summary>
    /// Gets the full path to migration scripts.
    /// </summary>
    public string GetMigrationsFullPath() => Path.Combine(ScriptsPath, MigrationsPath);

    /// <summary>
    /// Validates that all required configuration is present and valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is required. Please provide it via:\n" +
                "  - Command line: --connection-string \"your-connection-string\"\n" +
                "  - Environment variable: DB_CONNECTION_STRING\n" +
                "  - appsettings.json: ConnectionStrings:DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(ScriptsPath))
        {
            throw new InvalidOperationException("Migration:ScriptsPath is required in configuration");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Migration:TimeoutSeconds must be greater than 0");
        }
    }
}
