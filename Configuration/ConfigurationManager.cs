using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace JTDev.DbMigrator.Configuration;

/// <summary>
/// Manages configuration loading and validation for the database migrator.
/// Implements priority-based configuration resolution: CLI > Environment Variables > appsettings.json
/// </summary>
public class ConfigurationManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationManager> _logger;

    public ConfigurationManager(
        IConfiguration configuration,
        ILogger<ConfigurationManager> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads and validates migration options with priority resolution.
    /// Priority order: CLI arguments > Environment variables > appsettings.json
    /// </summary>
    /// <param name="requireScriptsPath">When false, skips ScriptsPath validation (e.g. in query-only mode).</param>
    /// <returns>Validated MigrationOptions instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or connection test fails</exception>
    public MigrationOptions LoadMigrationOptions(bool requireScriptsPath = true)
    {
        _logger.LogInformation("Loading migration configuration with priority: CLI > ENV > appsettings");

        var options = new MigrationOptions();

        // Load base configuration from appsettings.json
        _configuration.GetSection("Migration").Bind(options);

        // Priority 3: appsettings.json ConnectionStrings section
        var connectionStringFromSettings = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionStringFromSettings))
        {
            options.ConnectionString = connectionStringFromSettings;
            _logger.LogDebug("Connection string loaded from appsettings.json");
        }

        // Priority 2: Environment variable DB_CONNECTION_STRING
        var connectionStringFromEnv = _configuration.GetValue<string>("DB_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connectionStringFromEnv))
        {
            options.ConnectionString = connectionStringFromEnv;
            _logger.LogInformation("Connection string overridden by environment variable DB_CONNECTION_STRING");
        }

        // Priority 1: CLI argument --connection-string
        var connectionStringFromCli = _configuration.GetValue<string>("connection-string");
        if (!string.IsNullOrWhiteSpace(connectionStringFromCli))
        {
            options.ConnectionString = connectionStringFromCli;
            _logger.LogInformation("Connection string overridden by command line argument --connection-string");
        }

        // Validate configuration
        try
        {
            options.Validate(requireScriptsPath);
            _logger.LogInformation("Migration configuration validated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Migration configuration validation failed");
            throw;
        }

        // Log resolved configuration (masked connection string)
        LogResolvedConfiguration(options);

        return options;
    }

    /// <summary>
    /// Tests the database connection before performing migration operations.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string to test</param>
    /// <exception cref="InvalidOperationException">Thrown when connection test fails</exception>
    public async Task TestConnectionAsync(string connectionString)
    {
        _logger.LogInformation("Testing database connection...");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Execute simple query to verify connection is functional
            await using var command = new NpgsqlCommand("SELECT version();", connection);
            var version = await command.ExecuteScalarAsync() as string;

            _logger.LogInformation("Database connection test successful");
            _logger.LogInformation("PostgreSQL version: {Version}", version);
        }
        catch (NpgsqlException ex)
        {
            var errorMessage = "Database connection test failed. Please verify:\n" +
                               "  - Connection string is correct\n" +
                               "  - PostgreSQL server is running and accessible\n" +
                               "  - Database exists and credentials are valid\n" +
                               $"  - Error: {ex.Message}";

            _logger.LogError(ex, "Database connection test failed");
            throw new InvalidOperationException(errorMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database connection test");
            throw new InvalidOperationException($"Database connection test failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates that the migration scripts directory exists and contains expected structure.
    /// </summary>
    /// <param name="options">Migration options containing scripts path configuration</param>
    /// <exception cref="InvalidOperationException">Thrown when scripts directory structure is invalid</exception>
    public void ValidateScriptsDirectory(MigrationOptions options)
    {
        _logger.LogInformation("Validating migration scripts directory structure...");

        // Validate root scripts path
        if (!Directory.Exists(options.ScriptsPath))
        {
            throw new InvalidOperationException(
                $"Migration scripts root directory not found: {Path.GetFullPath(options.ScriptsPath)}");
        }

        // Validate schema directory
        var schemaPath = options.GetSchemaFullPath();
        if (!Directory.Exists(schemaPath))
        {
            _logger.LogWarning("Schema directory not found: {SchemaPath}", schemaPath);
        }

        // Validate seeds directory (optional)
        var seedsPath = options.GetSeedsFullPath();
        if (!Directory.Exists(seedsPath))
        {
            _logger.LogWarning("Seeds directory not found (optional): {SeedsPath}", seedsPath);
        }

        // Validate migrations directory (optional)
        var migrationsPath = options.GetMigrationsFullPath();
        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Migrations directory not found (optional): {MigrationsPath}", migrationsPath);
        }

        _logger.LogInformation("Migration scripts directory validation completed");
    }

    /// <summary>
    /// Logs the resolved configuration with sensitive data masked.
    /// </summary>
    private void LogResolvedConfiguration(MigrationOptions options)
    {
        var maskedConnectionString = MaskConnectionString(options.ConnectionString!);

        _logger.LogInformation("Resolved migration configuration:");
        _logger.LogInformation("  ConnectionString: {ConnectionString}", maskedConnectionString);
        _logger.LogInformation("  ScriptsPath: {ScriptsPath}",
            string.IsNullOrWhiteSpace(options.ScriptsPath) ? "[not set]" : Path.GetFullPath(options.ScriptsPath));
        _logger.LogInformation("  SchemaPath: {SchemaPath}", options.SchemaPath);
        _logger.LogInformation("  SeedsPath: {SeedsPath}", options.SeedsPath);
        _logger.LogInformation("  MigrationsPath: {MigrationsPath}", options.MigrationsPath);
        _logger.LogInformation("  TimeoutSeconds: {TimeoutSeconds}", options.TimeoutSeconds);
    }

    /// <summary>
    /// Masks sensitive information in connection string for logging.
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "[EMPTY]";

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            // Mask password
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "****";
            }

            return builder.ToString();
        }
        catch
        {
            // If parsing fails, just mask the entire string
            return "[MASKED]";
        }
    }
}
