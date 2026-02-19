namespace JTDev.DbMigrator.Cli;

/// <summary>
/// Represents the parsed command-line options for the database migrator.
/// </summary>
public class CliOptions
{
    /// <summary>
    /// Execute only schema scripts (DDL).
    /// </summary>
    public bool SchemaOnly { get; set; }

    /// <summary>
    /// Execute only migration scripts.
    /// </summary>
    public bool MigrationsOnly { get; set; }

    /// <summary>
    /// Execute only seed data scripts.
    /// </summary>
    public bool SeedsOnly { get; set; }

    /// <summary>
    /// Execute schema and migrations, but skip seed data.
    /// </summary>
    public bool SkipSeeds { get; set; }

    /// <summary>
    /// Override the connection string from configuration.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Display help message and exit.
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// Enable verbose logging output.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// SQL query to execute and display results.
    /// When specified, only the query is executed (no migrations).
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Gets whether this is a query-only execution (no migrations).
    /// </summary>
    public bool IsQueryMode => !string.IsNullOrWhiteSpace(Query);

    /// <summary>
    /// Gets whether schema scripts should be executed based on the current options.
    /// </summary>
    public bool ShouldExecuteSchema =>
        !MigrationsOnly && !SeedsOnly && !ShowHelp;

    /// <summary>
    /// Gets whether migration scripts should be executed based on the current options.
    /// </summary>
    public bool ShouldExecuteMigrations =>
        !SchemaOnly && !SeedsOnly && !ShowHelp;

    /// <summary>
    /// Gets whether seed scripts should be executed based on the current options.
    /// </summary>
    public bool ShouldExecuteSeeds =>
        !SchemaOnly && !MigrationsOnly && !SkipSeeds && !ShowHelp;

    /// <summary>
    /// Validates the options for logical consistency.
    /// </summary>
    /// <returns>True if options are valid; otherwise, false with error message.</returns>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        // In query mode, migration options are ignored
        if (IsQueryMode)
        {
            if (SchemaOnly || MigrationsOnly || SeedsOnly || SkipSeeds)
            {
                return (false, "Cannot combine --query with migration options (--schema-only, --migrations-only, --seeds-only, --skip-seeds)");
            }
            return (true, null);
        }

        // Count mutually exclusive options
        int exclusiveCount = 0;
        if (SchemaOnly) exclusiveCount++;
        if (MigrationsOnly) exclusiveCount++;
        if (SeedsOnly) exclusiveCount++;

        if (exclusiveCount > 1)
        {
            return (false, "Cannot specify multiple exclusive options: --schema-only, --migrations-only, --seeds-only");
        }

        if (SkipSeeds && SeedsOnly)
        {
            return (false, "Cannot specify both --skip-seeds and --seeds-only");
        }

        if (SkipSeeds && (SchemaOnly || MigrationsOnly))
        {
            return (false, "--skip-seeds is redundant with --schema-only or --migrations-only");
        }

        return (true, null);
    }
}
