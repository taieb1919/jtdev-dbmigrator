using JTDev.DbMigrator.Models;

namespace JTDev.DbMigrator.Data;

/// <summary>
/// Repository interface for managing migration tracking in schema_migrations table.
/// Ensures idempotent database migrations with checksum verification.
/// </summary>
public interface IMigrationRepository
{
    /// <summary>
    /// Ensures the schema_migrations table exists in the database.
    /// Creates the table if it doesn't exist (idempotent operation).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureTableExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a migration with the specified version has already been applied.
    /// </summary>
    /// <param name="version">Migration version (filename without extension)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the migration has been applied, false otherwise</returns>
    Task<bool> IsAppliedAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successfully applied migration in the schema_migrations table.
    /// </summary>
    /// <param name="version">Migration version (filename without extension)</param>
    /// <param name="checksum">MD5 checksum of the migration script content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordMigrationAsync(string version, string checksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all applied migrations ordered by applied_at timestamp.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of migration records</returns>
    Task<IEnumerable<MigrationRecord>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the checksum of an applied migration.
    /// </summary>
    /// <param name="version">Migration version to verify</param>
    /// <param name="expectedChecksum">Expected MD5 checksum</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the checksum matches, false otherwise</returns>
    Task<bool> VerifyChecksumAsync(string version, string expectedChecksum, CancellationToken cancellationToken = default);
}
