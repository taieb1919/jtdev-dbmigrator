namespace JTDev.DbMigrator.Cli;

/// <summary>
/// Simple command-line argument parser for the database migrator.
/// Supports both --option and --option=value formats without external dependencies.
/// </summary>
public static class CliArgumentParser
{
    /// <summary>
    /// Parses command-line arguments into a CliOptions object.
    /// </summary>
    /// <param name="args">The command-line arguments array.</param>
    /// <returns>A populated CliOptions instance.</returns>
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        if (args == null || args.Length == 0)
        {
            return options;
        }

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            // Ensure argument starts with - (single or double dash)
            if (!arg.StartsWith("-"))
            {
                Console.WriteLine($"Warning: Ignoring invalid argument '{arg}'. Arguments must start with '-'.");
                continue;
            }

            // Split on '=' to handle --option=value format
            var parts = arg.Split('=', 2);
            var optionName = parts[0].ToLowerInvariant();
            var optionValue = parts.Length > 1 ? parts[1] : null;

            switch (optionName)
            {
                case "--schema-only":
                    options.SchemaOnly = true;
                    break;

                case "--migrations-only":
                    options.MigrationsOnly = true;
                    break;

                case "--seeds-only":
                    options.SeedsOnly = true;
                    break;

                case "--skip-seeds":
                    options.SkipSeeds = true;
                    break;

                case "--connection-string":
                    if (string.IsNullOrWhiteSpace(optionValue))
                    {
                        Console.WriteLine("Error: --connection-string requires a value. Use --connection-string=<value>");
                        options.ShowHelp = true;
                    }
                    else
                    {
                        options.ConnectionString = optionValue;
                    }
                    break;

                case "--query":
                case "-q":
                    if (string.IsNullOrWhiteSpace(optionValue))
                    {
                        Console.WriteLine("Error: --query requires a SQL statement. Use --query=\"SELECT ...\"");
                        options.ShowHelp = true;
                    }
                    else
                    {
                        options.Query = optionValue;
                    }
                    break;

                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;

                case "--help":
                case "-h":
                case "-?":
                    options.ShowHelp = true;
                    break;

                default:
                    Console.WriteLine($"Warning: Unknown option '{optionName}'");
                    options.ShowHelp = true;
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Displays usage information and available command-line options.
    /// </summary>
    public static void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      JTDev Database Migration Engine                      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  JTDev.DbMigrator [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("DESCRIPTION:");
        Console.WriteLine("  Executes database schema, migrations, and seed scripts against PostgreSQL.");
        Console.WriteLine("  By default, all scripts (schema + migrations + seeds) are executed.");
        Console.WriteLine("  Can also execute ad-hoc SQL queries and display results.");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine();
        Console.WriteLine("  Execution Control:");
        Console.WriteLine("  --schema-only          Execute only schema scripts (DDL)");
        Console.WriteLine("  --migrations-only      Execute only migration scripts");
        Console.WriteLine("  --seeds-only           Execute only seed data scripts");
        Console.WriteLine("  --skip-seeds           Execute schema and migrations, but skip seeds");
        Console.WriteLine();
        Console.WriteLine("  Configuration:");
        Console.WriteLine("  --connection-string=<value>");
        Console.WriteLine("                         Override the database connection string");
        Console.WriteLine("                         Example: --connection-string=\"Host=localhost;...\"");
        Console.WriteLine();
        Console.WriteLine("  Query Mode:");
        Console.WriteLine("  --query=<sql>, -q=<sql>");
        Console.WriteLine("                         Execute a SQL query and display results in table format");
        Console.WriteLine("                         Example: --query=\"SELECT id, email FROM users LIMIT 10\"");
        Console.WriteLine("                         Note: When using --query, migration options are ignored");
        Console.WriteLine();
        Console.WriteLine("  Output:");
        Console.WriteLine("  --verbose, -v          Enable verbose logging output");
        Console.WriteLine("  --help, -h, -?         Display this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine();
        Console.WriteLine("  # Execute all scripts (default behavior)");
        Console.WriteLine("  JTDev.DbMigrator");
        Console.WriteLine();
        Console.WriteLine("  # Execute only schema scripts");
        Console.WriteLine("  JTDev.DbMigrator --schema-only");
        Console.WriteLine();
        Console.WriteLine("  # Execute schema and migrations, skip seeds");
        Console.WriteLine("  JTDev.DbMigrator --skip-seeds");
        Console.WriteLine();
        Console.WriteLine("  # Override connection string");
        Console.WriteLine("  JTDev.DbMigrator --connection-string=\"Host=prod-db;Port=5432;...\"");
        Console.WriteLine();
        Console.WriteLine("  # Verbose output with custom connection");
        Console.WriteLine("  JTDev.DbMigrator --verbose --connection-string=\"Host=localhost;...\"");
        Console.WriteLine();
        Console.WriteLine("  # Execute a SELECT query and display results");
        Console.WriteLine("  JTDev.DbMigrator --query=\"SELECT id, email, role FROM users LIMIT 5\"");
        Console.WriteLine();
        Console.WriteLine("  # Query with custom connection");
        Console.WriteLine("  JTDev.DbMigrator --query=\"SELECT COUNT(*) FROM schema_migrations\" --connection-string=\"...\"");
        Console.WriteLine();
        Console.WriteLine("NOTES:");
        Console.WriteLine("  - Options --schema-only, --migrations-only, and --seeds-only are mutually exclusive");
        Console.WriteLine("  - Scripts are executed in order: schema → migrations → seeds");
        Console.WriteLine("  - Connection string priority: CLI argument > Environment > appsettings.json");
        Console.WriteLine("  - Query mode (--query) cannot be combined with migration options");
        Console.WriteLine();
    }
}
