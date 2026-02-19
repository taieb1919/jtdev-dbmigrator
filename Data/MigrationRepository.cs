using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using JTDev.DbMigrator.Models;

namespace JTDev.DbMigrator.Data;

/// <summary>
/// Dapper-based repository for managing migration tracking in PostgreSQL.
/// Implements FEAT-001-R003 traceability rule with MD5 checksum verification.
/// </summary>
public class MigrationRepository : IMigrationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MigrationRepository> _logger;

    // SQL Scripts
    private const string CreateTableSql = @"
        CREATE TABLE IF NOT EXISTS schema_migrations (
            version VARCHAR(255) PRIMARY KEY,
            applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            checksum VARCHAR(32) NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_schema_migrations_applied_at
        ON schema_migrations(applied_at);";

    private const string IsAppliedSql = @"
        SELECT EXISTS(
            SELECT 1 FROM schema_migrations WHERE version = @Version
        );";

    private const string RecordMigrationSql = @"
        INSERT INTO schema_migrations (version, applied_at, checksum)
        VALUES (@Version, @AppliedAt, @Checksum)
        ON CONFLICT (version) DO NOTHING;";

    private const string GetAppliedMigrationsSql = @"
        SELECT version, applied_at, checksum
        FROM schema_migrations
        ORDER BY applied_at ASC;";

    private const string GetChecksumSql = @"
        SELECT checksum
        FROM schema_migrations
        WHERE version = @Version;";

    public MigrationRepository(string connectionString, ILogger<MigrationRepository> logger)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString), "Connection string is required");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    CreateTableSql,
                    cancellationToken: cancellationToken));

            _logger.LogInformation("Schema_migrations table verified/created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure schema_migrations table exists");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAppliedAsync(string version, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var exists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    IsAppliedSql,
                    new { Version = version },
                    cancellationToken: cancellationToken));

            _logger.LogDebug("Migration {Version} applied status: {Exists}", version, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if migration {Version} is applied", version);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecordMigrationAsync(string version, string checksum, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    RecordMigrationSql,
                    new
                    {
                        Version = version,
                        AppliedAt = DateTime.UtcNow,
                        Checksum = checksum
                    },
                    cancellationToken: cancellationToken));

            if (rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Migration {Version} recorded successfully with checksum {Checksum}",
                    version,
                    checksum);
            }
            else
            {
                _logger.LogWarning(
                    "Migration {Version} already exists in schema_migrations (no-op)",
                    version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record migration {Version}", version);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MigrationRecord>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var migrations = await connection.QueryAsync<MigrationRecord>(
                new CommandDefinition(
                    GetAppliedMigrationsSql,
                    cancellationToken: cancellationToken));

            _logger.LogInformation("Retrieved {Count} applied migrations", migrations.Count());
            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve applied migrations");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyChecksumAsync(string version, string expectedChecksum, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var actualChecksum = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    GetChecksumSql,
                    new { Version = version },
                    cancellationToken: cancellationToken));

            if (actualChecksum == null)
            {
                _logger.LogWarning("Migration {Version} not found in schema_migrations", version);
                return false;
            }

            var matches = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);

            if (!matches)
            {
                _logger.LogWarning(
                    "Checksum mismatch for migration {Version}. Expected: {Expected}, Actual: {Actual}",
                    version,
                    expectedChecksum,
                    actualChecksum);
            }

            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify checksum for migration {Version}", version);
            throw;
        }
    }
}
