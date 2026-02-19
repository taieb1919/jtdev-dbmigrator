using System.Security.Cryptography;
using System.Text;
using Npgsql;
using JTDev.DbMigrator.Cli;
using JTDev.DbMigrator.Configuration;
using JTDev.DbMigrator.Data;
using JTDev.DbMigrator.Logging;

namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Main migration engine that orchestrates the execution of database scripts.
/// Handles schema, migration, and seed scripts in proper order with transaction management.
/// </summary>
public class MigrationEngine : IMigrationEngine
{
    private readonly IConsoleLogger _logger;
    private readonly IMigrationRepository _repository;
    private readonly MigrationOptions _options;

    public MigrationEngine(
        IConsoleLogger logger,
        IMigrationRepository repository,
        MigrationOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<MigrationResult> ExecuteAsync(CliOptions cliOptions, CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();

        try
        {
            _logger.Info("=".PadRight(80, '='));
            _logger.Info("JTDev - Database Migration Engine");
            _logger.Info("=".PadRight(80, '='));
            _logger.Info("");

            // Ensure schema_migrations table exists
            _logger.Info("Ensuring schema_migrations tracking table exists...");
            await _repository.EnsureTableExistsAsync(cancellationToken);
            _logger.Success("Schema_migrations table ready");
            _logger.Info("");

            // Execute scripts in order based on CLI options
            if (cliOptions.ShouldExecuteSchema)
            {
                await ExecuteScriptTypeAsync(ScriptType.Schema, _options.GetSchemaFullPath(), result, cancellationToken);
                if (!result.IsSuccess) return result;
            }

            if (cliOptions.ShouldExecuteMigrations)
            {
                await ExecuteScriptTypeAsync(ScriptType.Migration, _options.GetMigrationsFullPath(), result, cancellationToken);
                if (!result.IsSuccess) return result;
            }

            if (cliOptions.ShouldExecuteSeeds)
            {
                await ExecuteScriptTypeAsync(ScriptType.Seed, _options.GetSeedsFullPath(), result, cancellationToken);
                if (!result.IsSuccess) return result;
            }

            // Print final summary
            _logger.Info("");
            _logger.Info("=".PadRight(80, '='));
            _logger.Info("Migration Summary");
            _logger.Info("=".PadRight(80, '='));
            _logger.Info($"Total Scripts:    {result.TotalScripts}");
            _logger.Success($"Executed:         {result.ExecutedScripts}");
            _logger.Skip($"Skipped:          {result.SkippedScripts}");

            if (result.FailedScripts > 0)
            {
                _logger.Error($"Failed:           {result.FailedScripts}");
            }
            else
            {
                _logger.Info($"Failed:           {result.FailedScripts}");
            }

            _logger.Info("=".PadRight(80, '='));

            if (result.IsSuccess)
            {
                _logger.Success("Migration completed successfully!");
            }
            else
            {
                _logger.Error("Migration failed with errors");
                foreach (var error in result.Errors)
                {
                    _logger.Error($"  - {error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Fatal error during migration execution", ex);
            result.AddError($"Fatal error: {ex.Message}");
            result.FailedScripts++;
        }

        return result;
    }

    /// <summary>
    /// Executes all scripts of a specific type from a directory
    /// </summary>
    private async Task ExecuteScriptTypeAsync(
        ScriptType scriptType,
        string directoryPath,
        MigrationResult result,
        CancellationToken cancellationToken)
    {
        _logger.Info($"Processing {scriptType} scripts from: {directoryPath}");
        _logger.Info("-".PadRight(80, '-'));

        // Check if directory exists
        if (!Directory.Exists(directoryPath))
        {
            _logger.Warning($"Directory not found: {directoryPath}");
            _logger.Info("");
            return;
        }

        // Get all .sql files sorted alphabetically
        var sqlFiles = Directory.GetFiles(directoryPath, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        if (sqlFiles.Count == 0)
        {
            _logger.Warning($"No .sql files found in {directoryPath}");
            _logger.Info("");
            return;
        }

        _logger.Info($"Found {sqlFiles.Count} script(s)");
        _logger.Info("");

        // Execute each script
        foreach (var scriptPath in sqlFiles)
        {
            var scriptName = Path.GetFileName(scriptPath);
            var version = Path.GetFileNameWithoutExtension(scriptName);

            result.TotalScripts++;

            // Check if already applied
            var isApplied = await _repository.IsAppliedAsync(version, cancellationToken);

            if (isApplied)
            {
                // Verify checksum to detect modified scripts (warn but still skip)
                var currentContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                var currentChecksum = CalculateMd5Checksum(currentContent);
                var checksumMatch = await _repository.VerifyChecksumAsync(version, currentChecksum, cancellationToken);

                if (!checksumMatch)
                {
                    _logger.Warning($"[WARN] {scriptName} - Already applied but script content has changed (checksum mismatch)!");
                }

                _logger.Skip($"[SKIP] {scriptName} - Already applied");
                result.SkippedScripts++;
                continue;
            }

            // Execute the script
            try
            {
                _logger.Info($"[RUN]  {scriptName}");

                // Read script content
                var scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);

                // Calculate checksum
                var checksum = CalculateMd5Checksum(scriptContent);

                // Execute script in transaction
                await ExecuteScriptAsync(scriptContent, cancellationToken);

                // Record successful migration
                await _repository.RecordMigrationAsync(version, checksum, cancellationToken);

                _logger.Success($"[OK]   {scriptName}");
                result.ExecutedScripts++;
            }
            catch (Exception ex)
            {
                _logger.Error($"[FAIL] {scriptName}");
                _logger.Error($"       Error: {ex.Message}");

                result.FailedScripts++;
                result.AddError($"{scriptName}: {ex.Message}");

                // Stop execution on first error
                _logger.Error("");
                _logger.Error("Stopping execution due to error");
                return;
            }
        }

        _logger.Info("");
    }

    /// <summary>
    /// Executes a SQL script within a transaction
    /// </summary>
    private async Task ExecuteScriptAsync(string scriptContent, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = new NpgsqlCommand(scriptContent, connection, transaction)
            {
                CommandTimeout = _options.TimeoutSeconds
            };

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Calculates MD5 checksum of script content for verification
    /// </summary>
    private static string CalculateMd5Checksum(string content)
    {
        using var md5 = MD5.Create();
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = md5.ComputeHash(contentBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
