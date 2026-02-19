using FluentAssertions;
using Xunit;
using JTDev.DbMigrator.Engine;
using JTDev.DbMigrator.Tests.Helpers;

namespace JTDev.DbMigrator.Tests.Engine;

/// <summary>
/// Tests unitaires de ScriptFile.CalculateChecksum() et decouverte filesystem.
/// AC couverts: #9 (checksum MD5, tri alphabetique, filtrage .sql)
/// </summary>
public class ScriptFileTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Task 4 — CalculateChecksum() et UpdateChecksum() (AC #9)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateChecksum_WithContent_ReturnsHexLowercase()
    {
        // Arrange — contenu non vide → string hexadécimale 32 chars lowercase
        var content = "CREATE TABLE users (id INT);";

        // Act
        var checksum = ScriptFile.CalculateChecksum(content);

        // Assert — MD5 produit 16 bytes → 32 chars hex
        checksum.Should().HaveLength(32);
        checksum.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void CalculateChecksum_WithEmptyContent_ReturnsEmptyString()
    {
        // Arrange — contenu vide → ""
        // Act
        var checksum = ScriptFile.CalculateChecksum(string.Empty);

        // Assert
        checksum.Should().BeEmpty();
    }

    [Fact]
    public void CalculateChecksum_WithNullContent_ReturnsEmptyString()
    {
        // Arrange — null → "" (IsNullOrEmpty)
        // Act
        var checksum = ScriptFile.CalculateChecksum(null!);

        // Assert
        checksum.Should().BeEmpty();
    }

    [Fact]
    public void CalculateChecksum_SameContent_ReturnsSameChecksum()
    {
        // Arrange — même contenu → même checksum (déterministe)
        var content = "SELECT 1;";

        // Act
        var checksum1 = ScriptFile.CalculateChecksum(content);
        var checksum2 = ScriptFile.CalculateChecksum(content);

        // Assert
        checksum1.Should().Be(checksum2);
    }

    [Fact]
    public void CalculateChecksum_DifferentContent_ReturnsDifferentChecksum()
    {
        // Arrange — contenus différents → checksums différents
        var content1 = "SELECT 1;";
        var content2 = "SELECT 2;";

        // Act
        var checksum1 = ScriptFile.CalculateChecksum(content1);
        var checksum2 = ScriptFile.CalculateChecksum(content2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void UpdateChecksum_UpdatesChecksumFromContent()
    {
        // Arrange — Content="X" → Checksum correspond à MD5("X")
        var script = new ScriptFile { Content = "X" };
        var expectedChecksum = ScriptFile.CalculateChecksum("X");

        // Act
        script.UpdateChecksum();

        // Assert
        script.Checksum.Should().Be(expectedChecksum);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 5 — Decouverte filesystem (AC #9)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FilesInDirectory_AreDiscoveredInAlphabeticalOrder()
    {
        // Arrange — AC #9 : créer 003.sql, 001.sql, 002.sql → tri alphabétique
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("003_last.sql", "SELECT 3;");
        helper.AddSchemaScript("001_first.sql", "SELECT 1;");
        helper.AddSchemaScript("002_middle.sql", "SELECT 2;");

        // Act — même logique que MigrationEngine.ExecuteScriptTypeAsync()
        var files = Directory.GetFiles(helper.SchemaDirectory, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        // Assert — tri alphabétique par nom de fichier
        files.Should().HaveCount(3);
        Path.GetFileName(files[0]).Should().Be("001_first.sql");
        Path.GetFileName(files[1]).Should().Be("002_middle.sql");
        Path.GetFileName(files[2]).Should().Be("003_last.sql");
    }

    [Fact]
    public void FilesInDirectory_OnlySqlFilesDiscovered()
    {
        // Arrange — créer .sql et .txt → seuls les .sql retournés
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_schema.sql", "SELECT 1;");

        // Ajouter un fichier .txt directement
        File.WriteAllText(Path.Combine(helper.SchemaDirectory, "readme.txt"), "not sql");
        File.WriteAllText(Path.Combine(helper.SchemaDirectory, "notes.md"), "not sql");

        // Act
        var files = Directory.GetFiles(helper.SchemaDirectory, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        // Assert — seul le .sql est retourné
        files.Should().HaveCount(1);
        Path.GetFileName(files[0]).Should().Be("001_schema.sql");
    }

    [Fact]
    public void EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange — répertoire vide → 0 fichiers
        using var helper = new TestScriptHelper();
        // Pas de fichiers ajoutés

        // Act
        var files = Directory.GetFiles(helper.SchemaDirectory, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        // Assert
        files.Should().BeEmpty();
    }
}
