namespace JTDev.DbMigrator.Models;

/// <summary>
/// Represents a migration record from the schema_migrations table.
/// Tracks executed database migrations for idempotency.
/// </summary>
public class MigrationRecord
{
    /// <summary>
    /// Migration version (filename without extension).
    /// Example: "01_initialize_schema"
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the migration was applied (UTC).
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// MD5 checksum of the migration script content.
    /// Used to detect modifications to already-applied migrations.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}
