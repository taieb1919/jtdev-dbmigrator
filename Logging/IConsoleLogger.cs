namespace JTDev.DbMigrator.Logging;

/// <summary>
/// Interface pour le logging colore dans la console
/// </summary>
public interface IConsoleLogger
{
    /// <summary>
    /// Log un message d'information (blanc/gris)
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Log un message de succes (vert)
    /// </summary>
    void Success(string message);

    /// <summary>
    /// Log un message d'operation ignoree (jaune)
    /// </summary>
    void Skip(string message);

    /// <summary>
    /// Log un message d'avertissement (jaune fonce)
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Log un message d'erreur (rouge)
    /// </summary>
    void Error(string message);

    /// <summary>
    /// Log une exception avec son stack trace (rouge)
    /// </summary>
    void Error(string message, Exception exception);

    /// <summary>
    /// Log un message avec un niveau de log specifique
    /// </summary>
    void Log(LogLevel level, string message);
}
