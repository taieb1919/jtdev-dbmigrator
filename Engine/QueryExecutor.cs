using System.Data;
using Npgsql;
using JTDev.DbMigrator.Logging;

namespace JTDev.DbMigrator.Engine;

/// <summary>
/// Executes SQL queries and displays results in a formatted table.
/// </summary>
public class QueryExecutor
{
    private readonly IConsoleLogger _logger;
    private readonly string _connectionString;
    private readonly int _timeoutSeconds;

    public QueryExecutor(IConsoleLogger logger, string connectionString, int timeoutSeconds = 300)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _timeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Executes a SQL query and displays the results in a formatted table.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>0 on success, 1 on error.</returns>
    public async Task<int> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("=".PadRight(80, '='));
            _logger.Info("JTDev - Query Executor");
            _logger.Info("=".PadRight(80, '='));
            _logger.Info("");
            _logger.Info("Executing query:");
            _logger.Info($"  {query}");
            _logger.Info("");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(query, connection)
            {
                CommandTimeout = _timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Get column information
            var columns = new List<ColumnInfo>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetName(i),
                    Type = reader.GetFieldType(i),
                    MaxWidth = reader.GetName(i).Length
                });
            }

            // Read all rows and calculate column widths
            var rows = new List<string[]>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new string[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    var value = reader.IsDBNull(i) ? "NULL" : FormatValue(reader.GetValue(i));
                    row[i] = value;
                    columns[i].MaxWidth = Math.Max(columns[i].MaxWidth, value.Length);
                }
                rows.Add(row);
            }

            // Display results
            DisplayTable(columns, rows);

            _logger.Info("");
            _logger.Success($"Query executed successfully. {rows.Count} row(s) returned.");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.Error("Query execution failed", ex);
            return 1;
        }
    }

    /// <summary>
    /// Formats a value for display.
    /// </summary>
    private static string FormatValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            bool b => b ? "true" : "false",
            byte[] bytes => $"[{bytes.Length} bytes]",
            decimal d => d.ToString("G"),
            double dbl => dbl.ToString("G"),
            float f => f.ToString("G"),
            _ => value.ToString() ?? "NULL"
        };
    }

    /// <summary>
    /// Displays the results in a formatted table.
    /// </summary>
    private void DisplayTable(List<ColumnInfo> columns, List<string[]> rows)
    {
        if (columns.Count == 0)
        {
            _logger.Warning("No columns returned.");
            return;
        }

        // Limit column width to 50 characters for readability
        const int MaxColumnWidth = 50;
        foreach (var col in columns)
        {
            col.MaxWidth = Math.Min(col.MaxWidth, MaxColumnWidth);
        }

        // Build separator line
        var separator = "+" + string.Join("+", columns.Select(c => new string('-', c.MaxWidth + 2))) + "+";

        // Build header
        var header = "|" + string.Join("|", columns.Select(c => $" {PadOrTruncate(c.Name, c.MaxWidth)} ")) + "|";

        // Display header
        Console.WriteLine(separator);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(header);
        Console.ResetColor();
        Console.WriteLine(separator);

        // Display rows
        if (rows.Count == 0)
        {
            var emptyRow = "|" + string.Join("|", columns.Select(c => $" {PadOrTruncate("(empty)", c.MaxWidth)} ")) + "|";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(emptyRow);
            Console.ResetColor();
        }
        else
        {
            foreach (var row in rows)
            {
                Console.Write("|");
                for (int i = 0; i < row.Length; i++)
                {
                    var value = row[i];
                    var col = columns[i];
                    var displayValue = PadOrTruncate(value, col.MaxWidth);

                    Console.Write(" ");
                    if (value == "NULL")
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(displayValue);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(displayValue);
                    }
                    Console.Write(" |");
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine(separator);
    }

    /// <summary>
    /// Pads or truncates a string to fit the specified width.
    /// </summary>
    private static string PadOrTruncate(string value, int width)
    {
        if (value.Length > width)
        {
            return value[..(width - 3)] + "...";
        }
        return value.PadRight(width);
    }

    private class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public Type Type { get; set; } = typeof(object);
        public int MaxWidth { get; set; }
    }
}
