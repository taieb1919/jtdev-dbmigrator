namespace JTDev.DbMigrator.Tests.Helpers;

/// <summary>
/// Helper pour créer des répertoires temporaires avec des fichiers .sql fictifs pour les tests.
/// Implémente IDisposable pour garantir le nettoyage même en cas d'échec de test.
/// </summary>
public sealed class TestScriptHelper : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Chemin du répertoire temporaire racine (contient schema/, migrations/, seeds/)
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Chemin du sous-répertoire schema/
    /// </summary>
    public string SchemaDirectory => Path.Combine(RootDirectory, "schema");

    /// <summary>
    /// Chemin du sous-répertoire migrations/
    /// </summary>
    public string MigrationsDirectory => Path.Combine(RootDirectory, "migrations");

    /// <summary>
    /// Chemin du sous-répertoire seeds/
    /// </summary>
    public string SeedsDirectory => Path.Combine(RootDirectory, "seeds");

    public TestScriptHelper()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), $"dbmigrator-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(SchemaDirectory);
        Directory.CreateDirectory(MigrationsDirectory);
        Directory.CreateDirectory(SeedsDirectory);
    }

    /// <summary>
    /// Crée un fichier .sql dans le répertoire schema/
    /// </summary>
    public string AddSchemaScript(string fileName, string content = "SELECT 1;")
    {
        var path = Path.Combine(SchemaDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Crée un fichier .sql dans le répertoire migrations/
    /// </summary>
    public string AddMigrationScript(string fileName, string content = "SELECT 1;")
    {
        var path = Path.Combine(MigrationsDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Crée un fichier .sql dans le répertoire seeds/
    /// </summary>
    public string AddSeedScript(string fileName, string content = "SELECT 1;")
    {
        var path = Path.Combine(SeedsDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Crée la structure complète par défaut avec des scripts dans chaque répertoire
    /// </summary>
    public void CreateDefaultStructure()
    {
        AddSchemaScript("001_create_users.sql", "CREATE TABLE users (id SERIAL PRIMARY KEY);");
        AddSchemaScript("002_create_tables.sql", "CREATE TABLE items (id SERIAL PRIMARY KEY);");
        AddMigrationScript("001_add_column.sql", "ALTER TABLE users ADD COLUMN email VARCHAR(255);");
        AddSeedScript("001_seed_data.sql", "INSERT INTO users (email) VALUES ('test@test.com');");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignorer les erreurs de nettoyage — le GC ou le OS s'en chargera
        }
    }
}
