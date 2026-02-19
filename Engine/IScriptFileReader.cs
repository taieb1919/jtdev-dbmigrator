namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Service for reading and processing SQL script files from the file system.
/// Provides cross-platform file system access with sorting and checksumming capabilities.
/// </summary>
public interface IScriptFileReader
{
    /// <summary>
    /// Gets all SQL scripts of the specified type, sorted alphabetically by filename.
    /// Returns an empty list if the directory does not exist.
    /// </summary>
    /// <param name="scriptType">Type of scripts to retrieve (Schema, Migration, or Seed)</param>
    /// <returns>Read-only list of script file metadata, sorted by filename</returns>
    IReadOnlyList<ScriptFile> GetScripts(ScriptType scriptType);

    /// <summary>
    /// Reads the content of a SQL script file asynchronously.
    /// Updates the script's Content and Checksum properties.
    /// </summary>
    /// <param name="script">Script file to read</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>SQL script content as string</returns>
    /// <exception cref="FileNotFoundException">Thrown when the script file does not exist</exception>
    Task<string> ReadContentAsync(ScriptFile script, CancellationToken ct = default);

    /// <summary>
    /// Calculates MD5 checksum of the provided content.
    /// Used for detecting changes in previously executed scripts.
    /// </summary>
    /// <param name="content">SQL script content</param>
    /// <returns>Hexadecimal MD5 checksum string (lowercase)</returns>
    string CalculateChecksum(string content);
}
