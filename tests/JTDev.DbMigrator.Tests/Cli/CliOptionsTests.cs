using FluentAssertions;
using Xunit;
using JTDev.DbMigrator.Cli;

namespace JTDev.DbMigrator.Tests.Cli;

/// <summary>
/// Tests unitaires de CliOptions — validation des combinaisons et propriétés computed.
/// AC couverts: #1 (combinaisons invalides), #2 (exclusive), #3 (skip-seeds), #4 (defaults), #5 (MigrationsOnly), #6 (IsQueryMode)
/// </summary>
public class CliOptionsTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Task 1 — Validate() combinaisons invalides (AC #1, #2, #3)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_SchemaOnlyAndMigrationsOnly_ReturnsFalseWithExclusiveMessage()
    {
        // Arrange — AC #1 : deux flags exclusifs → invalide avec message "exclusive"
        var options = new CliOptions { SchemaOnly = true, MigrationsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exclusive");
    }

    [Fact]
    public void Validate_SchemaOnlyAndSeedsOnly_ReturnsFalseWithExclusiveMessage()
    {
        // Arrange
        var options = new CliOptions { SchemaOnly = true, SeedsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exclusive");
    }

    [Fact]
    public void Validate_MigrationsOnlyAndSeedsOnly_ReturnsFalseWithExclusiveMessage()
    {
        // Arrange
        var options = new CliOptions { MigrationsOnly = true, SeedsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exclusive");
    }

    [Fact]
    public void Validate_AllThreeExclusive_ReturnsFalseWithExclusiveMessage()
    {
        // Arrange — les 3 flags exclusifs en même temps
        var options = new CliOptions { SchemaOnly = true, MigrationsOnly = true, SeedsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exclusive");
    }

    [Fact]
    public void Validate_QueryWithSchemaOnly_ReturnsFalseWithQueryMessage()
    {
        // Arrange — AC #2 : Query + option migration → invalide avec message "query"
        var options = new CliOptions { Query = "SELECT 1", SchemaOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("query");
    }

    [Fact]
    public void Validate_QueryWithMigrationsOnly_ReturnsFalseWithQueryMessage()
    {
        // Arrange
        var options = new CliOptions { Query = "SELECT 1", MigrationsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("query");
    }

    [Fact]
    public void Validate_QueryWithSeedsOnly_ReturnsFalseWithQueryMessage()
    {
        // Arrange
        var options = new CliOptions { Query = "SELECT 1", SeedsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("query");
    }

    [Fact]
    public void Validate_QueryWithSkipSeeds_ReturnsFalseWithQueryMessage()
    {
        // Arrange
        var options = new CliOptions { Query = "SELECT 1", SkipSeeds = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("query");
    }

    [Fact]
    public void Validate_SkipSeedsAndSeedsOnly_ReturnsFalse()
    {
        // Arrange — AC #3 : SkipSeeds + SeedsOnly → invalide
        var options = new CliOptions { SkipSeeds = true, SeedsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("skip-seeds");
        result.ErrorMessage.Should().Contain("seeds-only");
    }

    [Fact]
    public void Validate_SkipSeedsAndSchemaOnly_ReturnsFalseRedundant()
    {
        // Arrange — SkipSeeds + SchemaOnly → invalide car redundant
        var options = new CliOptions { SkipSeeds = true, SchemaOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("redundant");
    }

    [Fact]
    public void Validate_SkipSeedsAndMigrationsOnly_ReturnsFalseRedundant()
    {
        // Arrange — SkipSeeds + MigrationsOnly → invalide car redundant
        var options = new CliOptions { SkipSeeds = true, MigrationsOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("redundant");
    }

    [Fact]
    public void Validate_DefaultOptions_ReturnsTrue()
    {
        // Arrange — aucun flag → valide
        var options = new CliOptions();

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_QueryOnly_ReturnsTrue()
    {
        // Arrange — Query seul → valide
        var options = new CliOptions { Query = "SELECT 1" };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_SchemaOnlyAlone_ReturnsTrue()
    {
        // Arrange — SchemaOnly seul → valide
        var options = new CliOptions { SchemaOnly = true };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 2 — Propriétés computed (AC #4, #5, #6)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShouldExecuteSchema_DefaultOptions_ReturnsTrue()
    {
        // Arrange — AC #4 : défaut → ShouldExecuteSchema=true
        var options = new CliOptions();

        // Act & Assert
        options.ShouldExecuteSchema.Should().BeTrue();
    }

    [Fact]
    public void ShouldExecuteMigrations_DefaultOptions_ReturnsTrue()
    {
        // Arrange — AC #4 : défaut → ShouldExecuteMigrations=true
        var options = new CliOptions();

        // Act & Assert
        options.ShouldExecuteMigrations.Should().BeTrue();
    }

    [Fact]
    public void ShouldExecuteSeeds_DefaultOptions_ReturnsTrue()
    {
        // Arrange — AC #4 : défaut → ShouldExecuteSeeds=true
        var options = new CliOptions();

        // Act & Assert
        options.ShouldExecuteSeeds.Should().BeTrue();
    }

    [Fact]
    public void ShouldExecuteSchema_MigrationsOnly_ReturnsFalse()
    {
        // Arrange — AC #5 : MigrationsOnly → ShouldExecuteSchema=false
        var options = new CliOptions { MigrationsOnly = true };

        // Act & Assert
        options.ShouldExecuteSchema.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSchema_SeedsOnly_ReturnsFalse()
    {
        // Arrange — SeedsOnly → ShouldExecuteSchema=false
        var options = new CliOptions { SeedsOnly = true };

        // Act & Assert
        options.ShouldExecuteSchema.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteMigrations_SchemaOnly_ReturnsFalse()
    {
        // Arrange — SchemaOnly → ShouldExecuteMigrations=false
        var options = new CliOptions { SchemaOnly = true };

        // Act & Assert
        options.ShouldExecuteMigrations.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSeeds_SchemaOnly_ReturnsFalse()
    {
        // Arrange — SchemaOnly → ShouldExecuteSeeds=false
        var options = new CliOptions { SchemaOnly = true };

        // Act & Assert
        options.ShouldExecuteSeeds.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSeeds_SkipSeeds_ReturnsFalse()
    {
        // Arrange — SkipSeeds → ShouldExecuteSeeds=false
        var options = new CliOptions { SkipSeeds = true };

        // Act & Assert
        options.ShouldExecuteSeeds.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSeeds_MigrationsOnly_ReturnsFalse()
    {
        // Arrange — AC #5 : MigrationsOnly → ShouldExecuteSeeds=false
        var options = new CliOptions { MigrationsOnly = true };

        // Act & Assert
        options.ShouldExecuteSeeds.Should().BeFalse();
    }

    [Fact]
    public void IsQueryMode_WithQuery_ReturnsTrue()
    {
        // Arrange — AC #6 : Query non vide → IsQueryMode=true
        var options = new CliOptions { Query = "SELECT 1" };

        // Act & Assert
        options.IsQueryMode.Should().BeTrue();
    }

    [Fact]
    public void IsQueryMode_WithoutQuery_ReturnsFalse()
    {
        // Arrange — Query null → false
        var options = new CliOptions { Query = null };

        // Act & Assert
        options.IsQueryMode.Should().BeFalse();
    }

    [Fact]
    public void IsQueryMode_WithWhitespaceQuery_ReturnsFalse()
    {
        // Arrange — Query = "   " → false (IsNullOrWhiteSpace)
        var options = new CliOptions { Query = "   " };

        // Act & Assert
        options.IsQueryMode.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSchema_ShowHelp_ReturnsFalse()
    {
        // Arrange — ShowHelp → ShouldExecuteSchema=false
        var options = new CliOptions { ShowHelp = true };

        // Act & Assert
        options.ShouldExecuteSchema.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteMigrations_ShowHelp_ReturnsFalse()
    {
        // Arrange — ShowHelp → ShouldExecuteMigrations=false
        var options = new CliOptions { ShowHelp = true };

        // Act & Assert
        options.ShouldExecuteMigrations.Should().BeFalse();
    }

    [Fact]
    public void ShouldExecuteSeeds_ShowHelp_ReturnsFalse()
    {
        // Arrange — ShowHelp → ShouldExecuteSeeds=false
        var options = new CliOptions { ShowHelp = true };

        // Act & Assert
        options.ShouldExecuteSeeds.Should().BeFalse();
    }
}
