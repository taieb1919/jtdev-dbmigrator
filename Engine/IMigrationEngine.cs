using JTDev.DbMigrator.Cli;

namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Interface for the migration engine that orchestrates database script execution.
/// Handles schema, migration, and seed scripts in the correct order with proper transaction management.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    /// Executes database migrations based on the provided CLI options.
    /// Scripts are executed in order: Schema -> Migrations -> Seeds
    /// Each script runs in its own transaction for isolation and rollback capability.
    /// </summary>
    /// <param name="options">CLI options specifying which script types to execute</param>
    /// <param name="cancellationToken">Cancellation token to allow operation cancellation</param>
    /// <returns>
    /// A MigrationResult containing execution statistics and any errors encountered.
    /// IsSuccess will be false if any script fails or errors occur.
    /// </returns>
    /// <remarks>
    /// Execution stops immediately on the first error encountered.
    /// Already applied scripts are skipped based on the schema_migrations tracking table.
    /// </remarks>
    Task<MigrationResult> ExecuteAsync(CliOptions options, CancellationToken cancellationToken = default);
}
