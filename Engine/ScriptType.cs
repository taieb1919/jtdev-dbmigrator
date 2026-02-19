namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Represents the type of database script to execute.
/// Scripts are executed in the order: Schema -> Migration -> Seed
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// DDL schema scripts (tables, indexes, constraints)
    /// Executed first to establish database structure
    /// </summary>
    Schema,

    /// <summary>
    /// Migration scripts for schema changes and data migrations
    /// Executed after schema to modify existing structures
    /// </summary>
    Migration,

    /// <summary>
    /// Seed data scripts for populating initial/test data
    /// Executed last after schema and migrations are complete
    /// </summary>
    Seed
}
