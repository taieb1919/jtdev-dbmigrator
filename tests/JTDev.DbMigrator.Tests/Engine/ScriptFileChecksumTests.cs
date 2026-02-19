using FluentAssertions;
using Xunit;
using JTDev.DbMigrator.Engine;

namespace JTDev.DbMigrator.Tests.Engine;

/// <summary>
/// Tests unitaires de ScriptFile.CalculateChecksum() et UpdateChecksum().
/// AC couverts: #9 (checksum MD5 hexadécimal lowercase)
/// </summary>
public class ScriptFileChecksumTests
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
}
