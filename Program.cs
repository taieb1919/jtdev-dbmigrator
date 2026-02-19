using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JTDev.DbMigrator.Cli;
using JTDev.DbMigrator.Configuration;
using JTDev.DbMigrator.Data;
using JTDev.DbMigrator.Engine;
using JTDev.DbMigrator.Logging;

namespace JTDev.DbMigrator;

/// <summary>
/// Generic PostgreSQL database migration console application.
/// Manages database schema migrations, incremental migrations, and seed data.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        IConsoleLogger? consoleLogger = null;

        try
        {
            // Parse CLI arguments
            var cliOptions = CliArgumentParser.Parse(args);

            // Show help if requested
            if (cliOptions.ShowHelp)
            {
                CliArgumentParser.ShowHelp();
                return 0;
            }

            // Validate CLI options
            var (isValid, errorMessage) = cliOptions.Validate();
            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {errorMessage}");
                Console.ResetColor();
                Console.WriteLine();
                CliArgumentParser.ShowHelp();
                return 1;
            }

            // Build host with configuration and DI
            var host = CreateHostBuilder(args, cliOptions).Build();

            // Get console logger for colored output (singleton, no scope needed)
            consoleLogger = host.Services.GetRequiredService<IConsoleLogger>();

            // Load and validate migration configuration (singleton, loaded once)
            consoleLogger.Info("Loading configuration...");
            var migrationOptions = host.Services.GetRequiredService<MigrationOptions>();
            consoleLogger.Success("Configuration loaded successfully");
            consoleLogger.Info("");

            // Test database connection
            var configManager = host.Services.GetRequiredService<Configuration.ConfigurationManager>();
            await configManager.TestConnectionAsync(migrationOptions.ConnectionString!);
            consoleLogger.Info("");

            // Create a scope for scoped services
            using var scope = host.Services.CreateScope();
            var scopedServices = scope.ServiceProvider;

            // Query mode: execute query and display results
            if (cliOptions.IsQueryMode)
            {
                var queryExecutor = new QueryExecutor(
                    consoleLogger,
                    migrationOptions.ConnectionString!,
                    migrationOptions.TimeoutSeconds);

                return await queryExecutor.ExecuteQueryAsync(cliOptions.Query!);
            }

            // Migration mode: validate scripts directory and execute migrations
            configManager.ValidateScriptsDirectory(migrationOptions);
            consoleLogger.Info("");

            var migrationEngine = scopedServices.GetRequiredService<IMigrationEngine>();
            var result = await migrationEngine.ExecuteAsync(cliOptions);

            // Return exit code based on result
            return result.ExitCode;
        }
        catch (InvalidOperationException ex)
        {
            // Configuration or validation errors
            if (consoleLogger != null)
            {
                consoleLogger.Error(ex.Message);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Configuration Error: {ex.Message}");
                Console.ResetColor();
            }
            return 1;
        }
        catch (Exception ex)
        {
            // Unexpected errors
            if (consoleLogger != null)
            {
                consoleLogger.Error("Fatal error occurred", ex);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, CliOptions cliOptions) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment;

                config
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);

                // Override connection string from CLI if provided
                if (!string.IsNullOrWhiteSpace(cliOptions.ConnectionString))
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("connection-string", cliOptions.ConnectionString)
                    });
                }
            })
            .ConfigureServices((context, services) =>
            {
                // Register console logger (singleton for thread-safe colored output)
                services.AddSingleton<IConsoleLogger, ConsoleLogger>();

                // Register configuration manager (singleton — dependencies are all singleton)
                services.AddSingleton<Configuration.ConfigurationManager>();

                // Register migration options (singleton — loaded once with correct requireScriptsPath)
                services.AddSingleton<MigrationOptions>(sp =>
                {
                    var configManager = sp.GetRequiredService<Configuration.ConfigurationManager>();
                    return configManager.LoadMigrationOptions(requireScriptsPath: !cliOptions.IsQueryMode);
                });

                // Register migration repository (scoped)
                services.AddScoped<IMigrationRepository>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<MigrationRepository>>();
                    var options = sp.GetRequiredService<MigrationOptions>();
                    return new MigrationRepository(options.ConnectionString!, logger);
                });

                // Register migration engine (scoped)
                services.AddScoped<IMigrationEngine>(sp =>
                {
                    var logger = sp.GetRequiredService<IConsoleLogger>();
                    var repository = sp.GetRequiredService<IMigrationRepository>();
                    var options = sp.GetRequiredService<MigrationOptions>();
                    return new MigrationEngine(logger, repository, options);
                });
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();

                // Set log level based on verbose flag
                var verboseArg = args.Any(a =>
                    a.Equals("--verbose", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("-v", StringComparison.OrdinalIgnoreCase));

                logging.SetMinimumLevel(verboseArg ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Information);
            });
}
