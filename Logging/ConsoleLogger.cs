namespace JTDev.DbMigrator.Logging;

/// <summary>
/// Implementation du logger console avec sortie coloree thread-safe
/// Format: [HH:mm:ss] [LEVEL] Message
/// </summary>
public class ConsoleLogger : IConsoleLogger
{
    private readonly object _lock = new();

    private static readonly Dictionary<LogLevel, ConsoleColor> _colorMapping = new()
    {
        [LogLevel.Info] = ConsoleColor.Gray,
        [LogLevel.Success] = ConsoleColor.Green,
        [LogLevel.Skip] = ConsoleColor.Yellow,
        [LogLevel.Warning] = ConsoleColor.DarkYellow,
        [LogLevel.Error] = ConsoleColor.Red
    };

    public void Info(string message) => Log(LogLevel.Info, message);

    public void Success(string message) => Log(LogLevel.Success, message);

    public void Skip(string message) => Log(LogLevel.Skip, message);

    public void Warning(string message) => Log(LogLevel.Warning, message);

    public void Error(string message) => Log(LogLevel.Error, message);

    public void Error(string message, Exception exception)
    {
        Log(LogLevel.Error, message);
        Log(LogLevel.Error, $"Exception: {exception.GetType().Name}");
        Log(LogLevel.Error, $"Message: {exception.Message}");

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            Log(LogLevel.Error, "Stack Trace:");
            foreach (var line in exception.StackTrace.Split(Environment.NewLine))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Log(LogLevel.Error, $"  {line.Trim()}");
                }
            }
        }

        if (exception.InnerException != null)
        {
            Log(LogLevel.Error, "--- Inner Exception ---");
            Error(string.Empty, exception.InnerException);
        }
    }

    public void Log(LogLevel level, string message)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var levelText = level.ToString().ToUpperInvariant().PadRight(7);
            var formattedMessage = $"[{timestamp}] [{levelText}] {message}";

            if (_colorMapping.TryGetValue(level, out var color))
            {
                Console.ForegroundColor = color;
            }

            Console.WriteLine(formattedMessage);
            Console.ResetColor();
        }
    }
}
