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

    /// <summary>
    /// Crée un MigrationOptions avec une connection string invalide pour provoquer un échec SQL.
    /// L'ouverture de NpgsqlConnection échouera, permettant de tester les scénarios d'erreur.
    /// </summary>
    public static MigrationOptions CreateWithInvalidConnection(TestScriptHelper helper)
    {
        return CreateForTests(helper, "Host=invalid-host-that-does-not-exist;Database=nope;Username=x;Password=x;");
    }
}
