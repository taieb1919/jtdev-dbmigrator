using FluentAssertions;
using Xunit;
using JTDev.DbMigrator.Cli;

namespace JTDev.DbMigrator.Tests.Cli;

/// <summary>
/// Tests unitaires de CliArgumentParser.Parse() — parsing des arguments CLI.
/// AC couverts: #7 (--schema-only), #8 (--query=VALUE)
/// </summary>
public class CliArgumentParserTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Task 3 — Parse() cas de base (AC #7, #8)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyArgs_ReturnsDefaultOptions()
    {
        // Arrange — args vide → toutes les propriétés à false/null
        var args = Array.Empty<string>();

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SchemaOnly.Should().BeFalse();
        options.MigrationsOnly.Should().BeFalse();
        options.SeedsOnly.Should().BeFalse();
        options.SkipSeeds.Should().BeFalse();
        options.Verbose.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
        options.Query.Should().BeNull();
        options.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void Parse_NullArgs_ReturnsDefaultOptions()
    {
        // Arrange — args null → défaut
        // Act
        var options = CliArgumentParser.Parse(null!);

        // Assert
        options.SchemaOnly.Should().BeFalse();
        options.MigrationsOnly.Should().BeFalse();
        options.SeedsOnly.Should().BeFalse();
        options.SkipSeeds.Should().BeFalse();
        options.Verbose.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
        options.Query.Should().BeNull();
        options.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void Parse_SchemaOnlyFlag_SetsSchemaOnly()
    {
        // Arrange — AC #7 : --schema-only → SchemaOnly=true
        var args = new[] { "--schema-only" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SchemaOnly.Should().BeTrue();
        options.MigrationsOnly.Should().BeFalse();
        options.SeedsOnly.Should().BeFalse();
        options.SkipSeeds.Should().BeFalse();
    }

    [Fact]
    public void Parse_MigrationsOnlyFlag_SetsMigrationsOnly()
    {
        // Arrange
        var args = new[] { "--migrations-only" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.MigrationsOnly.Should().BeTrue();
        options.SchemaOnly.Should().BeFalse();
    }

    [Fact]
    public void Parse_SeedsOnlyFlag_SetsSeedsOnly()
    {
        // Arrange
        var args = new[] { "--seeds-only" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SeedsOnly.Should().BeTrue();
        options.SchemaOnly.Should().BeFalse();
    }

    [Fact]
    public void Parse_SkipSeedsFlag_SetsSkipSeeds()
    {
        // Arrange
        var args = new[] { "--skip-seeds" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SkipSeeds.Should().BeTrue();
    }

    [Fact]
    public void Parse_VerboseFlag_SetsVerbose()
    {
        // Arrange
        var args = new[] { "--verbose" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_VerboseShortFlag_SetsVerbose()
    {
        // Arrange — alias court -v
        var args = new[] { "-v" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_HelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_HelpShortH_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_HelpShortQuestion_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "-?" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_QueryWithValue_SetsQuery()
    {
        // Arrange — AC #8 : --query=SELECT 1 → Query="SELECT 1" et IsQueryMode=true
        var args = new[] { "--query=SELECT 1" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.Query.Should().Be("SELECT 1");
        options.IsQueryMode.Should().BeTrue();
    }

    [Fact]
    public void Parse_QueryShortAlias_SetsQuery()
    {
        // Arrange — alias court -q=SELECT 1
        var args = new[] { "-q=SELECT 1" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.Query.Should().Be("SELECT 1");
        options.IsQueryMode.Should().BeTrue();
    }

    [Fact]
    public void Parse_ConnectionStringWithValue_SetsConnectionString()
    {
        // Arrange
        var args = new[] { "--connection-string=Host=localhost;" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.ConnectionString.Should().Be("Host=localhost;");
    }

    [Fact]
    public void Parse_ConnectionStringWithMultipleEquals_PreservesFullValue()
    {
        // Arrange — connection string reelle avec multiples '=' (Split('=', 2) doit preserver)
        var connStr = "Host=localhost;Port=5432;Database=test;Username=admin";
        var args = new[] { $"--connection-string={connStr}" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.ConnectionString.Should().Be(connStr);
    }

    [Fact]
    public void Parse_QueryWithoutValue_SetsShowHelp()
    {
        // Arrange — --query sans valeur → ShowHelp=true, Query=null, message d'erreur emis
        var args = new[] { "--query" };
        using var output = new StringWriter();

        // Act
        var options = CliArgumentParser.Parse(args, output);

        // Assert
        options.ShowHelp.Should().BeTrue();
        options.Query.Should().BeNull();
        output.ToString().Should().Contain("--query requires a SQL statement");
    }

    [Fact]
    public void Parse_ConnectionStringWithoutValue_SetsShowHelp()
    {
        // Arrange — --connection-string sans valeur → ShowHelp=true, message d'erreur emis
        var args = new[] { "--connection-string" };
        using var output = new StringWriter();

        // Act
        var options = CliArgumentParser.Parse(args, output);

        // Assert
        options.ShowHelp.Should().BeTrue();
        options.ConnectionString.Should().BeNull();
        output.ToString().Should().Contain("--connection-string requires a value");
    }

    [Fact]
    public void Parse_UnknownOption_SetsShowHelp()
    {
        // Arrange — option inconnue → ShowHelp=true, warning emis
        var args = new[] { "--foobar" };
        using var output = new StringWriter();

        // Act
        var options = CliArgumentParser.Parse(args, output);

        // Assert
        options.ShowHelp.Should().BeTrue();
        output.ToString().Should().Contain("Unknown option");
    }

    [Fact]
    public void Parse_ArgumentWithoutDash_IgnoresArgument()
    {
        // Arrange — argument sans tiret → ignoré, options par défaut, warning emis
        var args = new[] { "foobar" };
        using var output = new StringWriter();

        // Act
        var options = CliArgumentParser.Parse(args, output);

        // Assert
        options.SchemaOnly.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
        options.Query.Should().BeNull();
        output.ToString().Should().Contain("Ignoring invalid argument");
    }

    [Fact]
    public void Parse_WhitespaceArgument_IgnoresArgument()
    {
        // Arrange — argument espace → ignoré
        var args = new[] { "   " };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SchemaOnly.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
    }

    [Fact]
    public void Parse_MultipleFlags_SetsAll()
    {
        // Arrange — plusieurs flags → tous setés
        var args = new[] { "--schema-only", "--verbose" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SchemaOnly.Should().BeTrue();
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_CaseInsensitive_ParsesCorrectly()
    {
        // Arrange — majuscules → parsé grâce à ToLowerInvariant()
        var args = new[] { "--SCHEMA-ONLY" };

        // Act
        var options = CliArgumentParser.Parse(args);

        // Assert
        options.SchemaOnly.Should().BeTrue();
    }
}
