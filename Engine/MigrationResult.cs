namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Represents the result of a migration execution operation.
/// Contains statistics and error information for the complete migration run.
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Total number of scripts discovered across all script types
    /// </summary>
    public int TotalScripts { get; set; }

    /// <summary>
    /// Number of scripts successfully executed
    /// </summary>
    public int ExecutedScripts { get; set; }

    /// <summary>
    /// Number of scripts skipped because they were already applied
    /// </summary>
    public int SkippedScripts { get; set; }

    /// <summary>
    /// Number of scripts that failed during execution
    /// </summary>
    public int FailedScripts { get; set; }

    /// <summary>
    /// Collection of error messages encountered during migration
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Indicates whether the migration completed successfully without errors
    /// </summary>
    public bool IsSuccess => FailedScripts == 0 && Errors.Count == 0;

    /// <summary>
    /// Gets the exit code that should be returned to the operating system.
    /// Returns 0 for success, non-zero for failure.
    /// </summary>
    public int ExitCode => IsSuccess ? 0 : 1;

    /// <summary>
    /// Adds an error message to the result
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Returns a formatted summary of the migration results
    /// </summary>
    public string GetSummary()
    {
        return $"Total: {TotalScripts}, Executed: {ExecutedScripts}, Skipped: {SkippedScripts}, Failed: {FailedScripts}";
    }
}
