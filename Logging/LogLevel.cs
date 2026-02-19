namespace JTDev.DbMigrator.Logging;

/// <summary>
/// Enumeration des niveaux de log pour la console
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Information generale (blanc/gris)
    /// </summary>
    Info,

    /// <summary>
    /// Operation reussie (vert)
    /// </summary>
    Success,

    /// <summary>
    /// Operation ignoree (jaune)
    /// </summary>
    Skip,

    /// <summary>
    /// Avertissement (jaune fonce)
    /// </summary>
    Warning,

    /// <summary>
    /// Erreur (rouge)
    /// </summary>
    Error
}
