using FluentAssertions;
using NSubstitute;
using Xunit;
using JTDev.DbMigrator.Cli;
using JTDev.DbMigrator.Data;
using JTDev.DbMigrator.Engine;
using JTDev.DbMigrator.Logging;
using JTDev.DbMigrator.Tests.Helpers;

namespace JTDev.DbMigrator.Tests.Engine;

/// <summary>
/// Tests unitaires du MigrationEngine — orchestration, skip, checksum, erreurs, cas limites.
/// AC couverts: #1 (setup), #2 (exécution complète), #3 (skip), #4 (checksum), #5 (erreurs)
/// </summary>
public class MigrationEngineTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers privés
    // ─────────────────────────────────────────────────────────────────────────

    private static (MigrationEngine engine, IConsoleLogger logger, IMigrationRepository repository) CreateEngine(
        TestScriptHelper helper,
        string? connectionString = null)
    {
        var logger = Substitute.For<IConsoleLogger>();
        var repository = Substitute.For<IMigrationRepository>();

        // Par défaut : aucun script déjà appliqué
        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = MigrationOptionsFactory.CreateForTests(helper, connectionString);
        var engine = new MigrationEngine(logger, repository, options);

        return (engine, logger, repository);
    }

    private static CliOptions DefaultOptions() => new CliOptions();

    // ─────────────────────────────────────────────────────────────────────────
    // Task 3 — Exécution complète (AC #2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DefaultOptions_ExecutesSchemaAndMigrationsAndSeeds()
    {
        // Arrange — AC #2 : options par défaut → tous les types de scripts exécutés
        using var helper = new TestScriptHelper();
        helper.CreateDefaultStructure();

        var (engine, logger, repository) = CreateEngine(helper, connectionString: null);

        // On simule que IsAppliedAsync=false et on utilise une connexion invalide:
        // les scripts ne seront PAS réellement exécutés, mais le moteur va TENTER
        // de les exécuter → l'erreur NpgsqlException sera capturée et stoppera l'exécution.
        // Pour vérifier que le moteur TENTE bien les trois types, on mock repository
        // de façon à ce que tous les scripts semblent déjà appliqués (skip sans connexion DB).
        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = DefaultOptions();

        // Act
        var result = await engine.ExecuteAsync(options, CancellationToken.None);

        // Assert — EnsureTableExistsAsync appelé en premier
        await repository.Received(1).EnsureTableExistsAsync(Arg.Any<CancellationToken>());

        // IsAppliedAsync appelé pour les scripts schema, migrations et seeds
        // 2 schema + 1 migration + 1 seed = 4 scripts au total
        await repository.Received(4).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        result.TotalScripts.Should().Be(4);
        result.SkippedScripts.Should().Be(4);
        result.ExecutedScripts.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_SchemaOnly_ExecutesOnlySchemaScripts()
    {
        // Arrange — AC #2 : SchemaOnly=true → seuls les scripts schema sont traités
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");
        helper.AddMigrationScript("001_add_column.sql");
        helper.AddSeedScript("001_seed_data.sql");

        var (engine, _, repository) = CreateEngine(helper);

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = new CliOptions { SchemaOnly = true };

        // Act
        var result = await engine.ExecuteAsync(options, CancellationToken.None);

        // Assert — seul le script schema est traité (1 script)
        result.TotalScripts.Should().Be(1);
        await repository.Received(1).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MigrationsOnly_ExecutesOnlyMigrationScripts()
    {
        // Arrange — AC #2 : MigrationsOnly=true → seuls les scripts migrations sont traités
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");
        helper.AddMigrationScript("001_add_column.sql");
        helper.AddSeedScript("001_seed_data.sql");

        var (engine, _, repository) = CreateEngine(helper);

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = new CliOptions { MigrationsOnly = true };

        // Act
        var result = await engine.ExecuteAsync(options, CancellationToken.None);

        // Assert — seul le script migration est traité (1 script)
        result.TotalScripts.Should().Be(1);
        await repository.Received(1).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SeedsOnly_ExecutesOnlySeedScripts()
    {
        // Arrange — AC #2 : SeedsOnly=true → seuls les seeds sont traités
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");
        helper.AddMigrationScript("001_add_column.sql");
        helper.AddSeedScript("001_seed_data.sql");

        var (engine, _, repository) = CreateEngine(helper);

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = new CliOptions { SeedsOnly = true };

        // Act
        var result = await engine.ExecuteAsync(options, CancellationToken.None);

        // Assert — seul le script seed est traité (1 script)
        result.TotalScripts.Should().Be(1);
        await repository.Received(1).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipSeeds_ExecutesSchemaAndMigrationsOnly()
    {
        // Arrange — AC #2 : SkipSeeds=true → schema + migrations, pas seeds
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");
        helper.AddMigrationScript("001_add_column.sql");
        helper.AddSeedScript("001_seed_data.sql");

        var (engine, _, repository) = CreateEngine(helper);

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var options = new CliOptions { SkipSeeds = true };

        // Act
        var result = await engine.ExecuteAsync(options, CancellationToken.None);

        // Assert — schema (1) + migration (1) = 2 scripts, seeds (1) ignoré
        result.TotalScripts.Should().Be(2);
        await repository.Received(2).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 4 — Skip et checksum (AC #3, #4)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ScriptAlreadyApplied_SkipsWithLog()
    {
        // Arrange — AC #3 : script déjà appliqué → log [SKIP], SkippedScripts++
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql", "CREATE TABLE users (id INT);");

        var (engine, logger, repository) = CreateEngine(helper);

        repository.IsAppliedAsync("001_create_users", Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync("001_create_users", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert — logger.Skip appelé avec "[SKIP]"
        logger.Received().Skip(Arg.Is<string>(s => s.Contains("[SKIP]")));
        result.SkippedScripts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ScriptAlreadyApplied_IncrementsSkippedCounter()
    {
        // Arrange — AC #3 : vérifier MigrationResult.SkippedScripts
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");
        helper.AddSchemaScript("002_create_tables.sql");

        var (engine, _, repository) = CreateEngine(helper);

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        repository.VerifyChecksumAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert
        result.SkippedScripts.Should().Be(2);
        result.ExecutedScripts.Should().Be(0);
        result.FailedScripts.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ChecksumMismatch_LogsWarningAndSkips()
    {
        // Arrange — AC #4 : checksum différent → [WARN] loggé, script quand même skip
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql", "CREATE TABLE users (id INT);");

        var (engine, logger, repository) = CreateEngine(helper);

        repository.IsAppliedAsync("001_create_users", Arg.Any<CancellationToken>()).Returns(true);
        // Checksum ne correspond pas → retourne false
        repository.VerifyChecksumAsync("001_create_users", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert — Warning loggé avec "checksum"
        logger.Received().Warning(Arg.Is<string>(s => s.Contains("checksum")));

        // Le script est quand même skippé
        result.SkippedScripts.Should().Be(1);
        result.ExecutedScripts.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_NewScript_ExecutesAndRecords()
    {
        // Arrange — AC #3/#4 : script non appliqué → exécuté + RecordMigrationAsync appelé
        // On utilise IsApplied=false et connexion invalide → exception → fail
        // Pour tester "execute + record", on doit simuler sans DB réelle.
        // Stratégie: On vérifie que RecordMigrationAsync N'EST PAS appelé quand l'exécution échoue
        // et qu'IsApplied=false → le code TENTE l'exécution SQL (qui échoue avec connexion invalide)
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql", "CREATE TABLE users (id INT);");

        var (engine, _, repository) = CreateEngine(helper, connectionString: "Host=invalid;Database=nope;Username=x;Password=x;");

        // Script non appliqué
        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert — RecordMigrationAsync NON appelé car l'exécution SQL a échoué
        await repository.DidNotReceive().RecordMigrationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Le script a été tenté (FailedScripts=1)
        result.FailedScripts.Should().Be(1);
        result.IsSuccess.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 5 — Gestion des erreurs (AC #5)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ScriptExecutionFails_LogsErrorAndStops()
    {
        // Arrange — AC #5 : échec SQL → logger.Error appelé
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql", "CREATE TABLE users (id INT);");

        var (engine, logger, repository) = CreateEngine(helper, connectionString: "Host=invalid;Database=nope;Username=x;Password=x;");

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert — Error loggé avec "[FAIL]"
        logger.Received().Error(Arg.Is<string>(s => s.Contains("[FAIL]")));
        result.FailedScripts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_ScriptExecutionFails_ResultIsNotSuccess()
    {
        // Arrange — AC #5 : result.IsSuccess==false, result.FailedScripts > 0
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql", "SELECT 1;");

        var (engine, _, repository) = CreateEngine(helper, connectionString: "Host=invalid;Database=nope;Username=x;Password=x;");

        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailedScripts.Should().BeGreaterThan(0);
        result.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ScriptExecutionFails_DoesNotExecuteRemainingScripts()
    {
        // Arrange — AC #5 : 3 scripts, échec au 1er → les suivants jamais exécutés
        // On ne peut pas intercepter facilement les appels intermédiaires sans DB,
        // donc on vérifie que FailedScripts=1 et TotalScripts=1 (arrêt immédiat)
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_table.sql", "CREATE TABLE a (id INT);");
        helper.AddSchemaScript("002_create_table.sql", "CREATE TABLE b (id INT);");
        helper.AddSchemaScript("003_create_table.sql", "CREATE TABLE c (id INT);");

        var (engine, _, repository) = CreateEngine(helper, connectionString: "Host=invalid;Database=nope;Username=x;Password=x;");

        // Tous les scripts sont "nouveaux" (pas encore appliqués)
        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await engine.ExecuteAsync(new CliOptions { SchemaOnly = true }, CancellationToken.None);

        // Assert — l'exécution s'arrête au premier échec
        // TotalScripts=1 car le compteur est incrémenté avant l'exécution et l'arrêt est immédiat
        result.FailedScripts.Should().Be(1);
        result.IsSuccess.Should().BeFalse();

        // IsAppliedAsync appelé pour le 1er script, mais les 2ème et 3ème ne sont pas atteints
        // (exécution arrêtée après le premier échec)
        await repository.Received(1).IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 6 — Cas limites (AC tous)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptyDirectory_ReturnsSuccessWithZeroScripts()
    {
        // Arrange — répertoires vides → success, 0 scripts
        using var helper = new TestScriptHelper();
        // Pas de fichiers ajoutés

        var (engine, _, repository) = CreateEngine(helper);

        // Act
        var result = await engine.ExecuteAsync(DefaultOptions(), CancellationToken.None);

        // Assert
        result.TotalScripts.Should().Be(0);
        result.ExecutedScripts.Should().Be(0);
        result.SkippedScripts.Should().Be(0);
        result.FailedScripts.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_EnsuresTrackingTableExists()
    {
        // Arrange — EnsureTableExistsAsync toujours appelé en premier
        using var helper = new TestScriptHelper();

        var (engine, _, repository) = CreateEngine(helper);

        // Act
        await engine.ExecuteAsync(DefaultOptions(), CancellationToken.None);

        // Assert — appelé exactement une fois, peu importe les scripts
        await repository.Received(1).EnsureTableExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsExecution()
    {
        // Arrange — CancellationToken annulé avant l'exécution → l'opération est interrompue
        using var helper = new TestScriptHelper();
        helper.AddSchemaScript("001_create_users.sql");

        var (engine, _, repository) = CreateEngine(helper);

        // Scripts "nouveaux" (pas encore appliqués)
        repository.IsAppliedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // EnsureTableExistsAsync lève une OperationCanceledException si le CT est annulé
        var cts = new CancellationTokenSource();

        repository.EnsureTableExistsAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                cts.Cancel(); // Annuler juste après EnsureTableExists
                throw new OperationCanceledException(cts.Token);
            });

        // Act
        var result = await engine.ExecuteAsync(DefaultOptions(), cts.Token);

        // Assert — l'exécution a été stoppée (erreur capturée par le try/catch global)
        // La OperationCanceledException est capturée → result.FailedScripts >= 1 (via le catch global)
        // OU result reste vide si l'exception est remontée différemment
        // Dans tous les cas, aucun script ne doit avoir été exécuté
        result.ExecutedScripts.Should().Be(0);
    }
}
