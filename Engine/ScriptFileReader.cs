using Microsoft.Extensions.Logging;
using JTDev.DbMigrator.Configuration;

namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Cross-platform SQL script file reader with sorting and checksumming capabilities.
/// Reads scripts from configurable directories and maintains execution metadata.
/// </summary>
public class ScriptFileReader : IScriptFileReader
{
    private readonly MigrationOptions _options;
    private readonly ILogger<ScriptFileReader> _logger;

    /// <summary>
    /// Initializes a new instance of ScriptFileReader.
    /// </summary>
    /// <param name="options">Migration configuration containing script paths</param>
    /// <param name="logger">Logger for diagnostic messages</param>
    public ScriptFileReader(MigrationOptions options, ILogger<ScriptFileReader> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<ScriptFile> GetScripts(ScriptType scriptType)
    {
        var directoryPath = GetDirectoryPath(scriptType);

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning(
                "Script directory does not exist: {DirectoryPath} (ScriptType: {ScriptType})",
                directoryPath,
                scriptType);
            return Array.Empty<ScriptFile>();
        }

        try
        {
            var sqlFiles = Directory.GetFiles(directoryPath, "*.sql", SearchOption.TopDirectoryOnly);

            if (sqlFiles.Length == 0)
            {
                _logger.LogInformation(
                    "No SQL scripts found in directory: {DirectoryPath} (ScriptType: {ScriptType})",
                    directoryPath,
                    scriptType);
                return Array.Empty<ScriptFile>();
            }

            // Sort alphabetically by filename for deterministic execution order
            var sortedFiles = sqlFiles
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();

            var scriptFiles = sortedFiles.Select(filePath =>
            {
                var fileName = Path.GetFileName(filePath);
                var version = Path.GetFileNameWithoutExtension(filePath);

                return new ScriptFile
                {
                    FileName = fileName,
                    Version = version,
                    FullPath = Path.GetFullPath(filePath), // Normalize to absolute path
                    ScriptType = scriptType,
                    Content = string.Empty, // Lazy-loaded
                    Checksum = string.Empty // Calculated when content is loaded
                };
            }).ToList();

            _logger.LogInformation(
                "Found {Count} SQL scripts in {DirectoryPath} (ScriptType: {ScriptType})",
                scriptFiles.Count,
                directoryPath,
                scriptType);

            return scriptFiles.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error reading scripts from directory: {DirectoryPath} (ScriptType: {ScriptType})",
                directoryPath,
                scriptType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> ReadContentAsync(ScriptFile script, CancellationToken ct = default)
    {
        if (script == null)
            throw new ArgumentNullException(nameof(script));

        if (!File.Exists(script.FullPath))
        {
            throw new FileNotFoundException(
                $"SQL script file not found: {script.FullPath}",
                script.FullPath);
        }

        try
        {
            _logger.LogDebug(
                "Reading script content: {FileName} ({ScriptType})",
                script.FileName,
                script.ScriptType);

            var content = await File.ReadAllTextAsync(script.FullPath, ct);

            // Update script object with content and checksum
            script.Content = content;
            script.Checksum = CalculateChecksum(content);

            _logger.LogDebug(
                "Script loaded successfully: {FileName} (Size: {Size} bytes, Checksum: {Checksum})",
                script.FileName,
                content.Length,
                script.Checksum);

            return content;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(
                ex,
                "Error reading script file: {FilePath}",
                script.FullPath);
            throw;
        }
    }

    /// <inheritdoc />
    public string CalculateChecksum(string content)
    {
        return ScriptFile.CalculateChecksum(content);
    }

    /// <summary>
    /// Gets the directory path for the specified script type.
    /// Uses Path.Combine for cross-platform compatibility.
    /// </summary>
    /// <param name="scriptType">Type of script (Schema, Migration, or Seed)</param>
    /// <returns>Absolute directory path</returns>
    private string GetDirectoryPath(ScriptType scriptType)
    {
        return scriptType switch
        {
            ScriptType.Schema => _options.GetSchemaFullPath(),
            ScriptType.Migration => _options.GetMigrationsFullPath(),
            ScriptType.Seed => _options.GetSeedsFullPath(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(scriptType),
                scriptType,
                $"Unknown script type: {scriptType}")
        };
    }
}
