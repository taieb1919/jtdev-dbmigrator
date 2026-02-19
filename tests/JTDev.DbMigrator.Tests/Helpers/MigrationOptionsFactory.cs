using JTDev.DbMigrator.Configuration;

namespace JTDev.DbMigrator.Tests.Helpers;

/// <summary>
/// Factory pour créer des instances de MigrationOptions pointant vers des répertoires temporaires.
/// </summary>
public static class MigrationOptionsFactory
{
    /// <summary>
    /// Crée un MigrationOptions pointant vers le répertoire racine du TestScriptHelper.
    /// ConnectionString par défaut = null (invalide, pour tester les erreurs d'exécution SQL).
    /// </summary>
    public static MigrationOptions CreateForTests(TestScriptHelper helper, string? connectionString = null)
    {
        return new MigrationOptions
        {
            ConnectionString = connectionString,
            ScriptsPath = helper.RootDirectory,
            SchemaPath = "schema",
            MigrationsPath = "migrations",
            SeedsPath = "seeds",
            TimeoutSeconds = 5
        };
    }
}
