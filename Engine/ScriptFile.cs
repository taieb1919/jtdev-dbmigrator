using System.Security.Cryptography;
using System.Text;

namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Represents a SQL script file with metadata for database migration execution.
/// </summary>
public class ScriptFile
{
    /// <summary>
    /// Name of the SQL file (e.g., "01_initialize_schema.sql")
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Version extracted from filename without extension (e.g., "01_initialize_schema")
    /// Used for tracking migration history.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Full absolute file system path to the SQL script.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Type of script (Schema, Migration, or Seed).
    /// Determines execution order and behavior.
    /// </summary>
    public ScriptType ScriptType { get; set; }

    /// <summary>
    /// SQL script content. Lazy-loaded via IScriptFileReader.ReadContentAsync().
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// MD5 checksum of the script content.
    /// Used to detect changes in previously executed scripts.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Calculates the MD5 checksum of the provided content.
    /// </summary>
    /// <param name="content">SQL script content</param>
    /// <returns>Hexadecimal MD5 checksum string</returns>
    public static string CalculateChecksum(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Updates the checksum based on the current content.
    /// </summary>
    public void UpdateChecksum()
    {
        Checksum = CalculateChecksum(Content);
    }
}
