using FluentAssertions;
using Xunit;
using JTDev.DbMigrator.Engine;
using JTDev.DbMigrator.Tests.Helpers;

namespace JTDev.DbMigrator.Tests.Engine;

/// <summary>
/// Tests unitaires de ScriptFile.DiscoverScripts() — decouverte filesystem.
/// AC couverts: #9 (tri alphabetique, filtrage .sql, repertoire vide)
/// </summary>
public class ScriptFileDiscoveryTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Task 5 — Decouverte filesystem via ScriptFile.DiscoverScripts() (AC #9)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DiscoverScripts_FilesInDirectory_AreReturnedInAlphabeticalOrder()
    {
        // Arrange — AC #9 : créer 003.sql, 001.sql, 002.sql → tri alphabétique
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("003_last.sql", "SELECT 3;");
        helper.AddSchemaScript("001_first.sql", "SELECT 1;");
        helper.AddSchemaScript("002_middle.sql", "SELECT 2;");

        // Act
        var files = ScriptFile.DiscoverScripts(helper.SchemaDirectory);

        // Assert — tri alphabétique par nom de fichier
        files.Should().HaveCount(3);
        Path.GetFileName(files[0]).Should().Be("001_first.sql");
        Path.GetFileName(files[1]).Should().Be("002_middle.sql");
        Path.GetFileName(files[2]).Should().Be("003_last.sql");
    }

    [Fact]
    public void DiscoverScripts_DirectoryContainsNonSqlFiles_OnlySqlFilesReturned()
    {
        // Arrange — créer .sql et .txt → seuls les .sql retournés
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_schema.sql", "SELECT 1;");

        // Ajouter des fichiers non-.sql directement
        File.WriteAllText(Path.Combine(helper.SchemaDirectory, "readme.txt"), "not sql");
        File.WriteAllText(Path.Combine(helper.SchemaDirectory, "notes.md"), "not sql");

        // Act
        var files = ScriptFile.DiscoverScripts(helper.SchemaDirectory);

        // Assert — seul le .sql est retourné
        files.Should().HaveCount(1);
        Path.GetFileName(files[0]).Should().Be("001_schema.sql");
    }

    [Fact]
    public void DiscoverScripts_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange — répertoire vide → 0 fichiers
        using var helper = new TestScriptHelper();
        // Pas de fichiers ajoutés

        // Act
        var files = ScriptFile.DiscoverScripts(helper.SchemaDirectory);

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverScripts_NonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange — répertoire inexistant → liste vide (pas d'exception)
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid():N}");

        // Act
        var files = ScriptFile.DiscoverScripts(nonExistentPath);

        // Assert
        files.Should().BeEmpty();
    }
}
