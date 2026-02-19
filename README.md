# JTDev.DbMigrator

Generic PostgreSQL database migration engine for .NET — schema DDL, incremental migrations, and seed data with checksum tracking and idempotency.

## Objectif

Outil de migration de base de données PostgreSQL réutilisable pour n'importe quel projet .NET. Gère l'application ordonnée et idempotente de scripts SQL avec tracking via une table `schema_migrations`.

## Installation

### Option 1 — Git submodule

```bash
git submodule add file:///path/to/jtdev-dbmigrator-remote.git tools/JTDev.DbMigrator
# Ou depuis un remote GitHub:
git submodule add https://github.com/jtdev/jtdev-dbmigrator.git tools/JTDev.DbMigrator
```

### Option 2 — Clone direct

```bash
git clone https://github.com/jtdev/jtdev-dbmigrator.git
```

### Option 3 — Référence projet dans .sln

Ajouter le projet au fichier `.sln` et référencer depuis votre projet:
```xml
<ProjectReference Include="../tools/JTDev.DbMigrator/JTDev.DbMigrator.csproj" />
```

## Configuration

### appsettings.json

```json
{
  "Migration": {
    "ScriptsPath": "/path/to/your/scripts",
    "SchemaPath": "schema",
    "SeedsPath": "seeds",
    "MigrationsPath": "migrations",
    "TimeoutSeconds": 300
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

| Paramètre | Description | Défaut |
|-----------|-------------|--------|
| `ScriptsPath` | Chemin racine vers les scripts SQL (requis) | `""` |
| `SchemaPath` | Sous-répertoire des scripts DDL | `"schema"` |
| `MigrationsPath` | Sous-répertoire des migrations incrémentales | `"migrations"` |
| `SeedsPath` | Sous-répertoire des données de seed | `"seeds"` |
| `TimeoutSeconds` | Timeout en secondes par opération DB | `300` |

### Hiérarchie de configuration

La connection string est résolue dans l'ordre de priorité suivant :

```
CLI --connection-string > ENV DB_CONNECTION_STRING > appsettings.json ConnectionStrings:DefaultConnection
```

1. **CLI** : `--connection-string="Host=localhost;..."`
2. **Variable d'environnement** : `DB_CONNECTION_STRING=Host=localhost;...`
3. **appsettings.json** : `ConnectionStrings:DefaultConnection`

## Commandes CLI

### Exécution standard

```bash
# Exécute schema + migrations + seeds (comportement par défaut)
dotnet run

# DDL uniquement (scripts dans schema/)
dotnet run -- --schema-only

# Migrations incrémentales uniquement
dotnet run -- --migrations-only

# Seeds uniquement
dotnet run -- --seeds-only

# Schema + migrations, sans seeds
dotnet run -- --skip-seeds
```

### Configuration à la volée

```bash
# Override connection string
dotnet run -- --connection-string="Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword"

# Logs verbeux (niveau Debug)
dotnet run -- --verbose

# Combinaison
dotnet run -- --skip-seeds --verbose --connection-string="Host=prod-db;..."
```

### Mode requête ad-hoc

```bash
# Exécute une requête SQL et affiche les résultats en tableau
dotnet run -- --query="SELECT * FROM users LIMIT 5"

# Avec connection string personnalisée
dotnet run -- --query="SELECT COUNT(*) FROM schema_migrations" --connection-string="Host=localhost;..."
```

### Aide

```bash
dotnet run -- --help
dotnet run -- -h
dotnet run -- -?
```

## Structure de scripts attendue

```
<ScriptsPath>/
├── schema/          # Scripts DDL (tables, index, contraintes)
│   ├── 00_create_schema.sql
│   └── 01_create_tables.sql
├── migrations/      # Changements incrémentaux
│   ├── 001_add_column.sql
│   └── 002_create_index.sql
└── seeds/           # Données initiales / de test
    ├── 01_reference_data.sql
    └── 02_test_users.sql
```

- Chaque type est exécuté en ordre **alphabétique**
- Ordre d'exécution global : `schema → migrations → seeds`
- Numérotation recommandée : 2 chiffres pour schema/seeds, 3 chiffres pour migrations

## Table de tracking `schema_migrations`

L'outil crée automatiquement une table `schema_migrations` pour tracker les migrations appliquées :

```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(255) PRIMARY KEY,   -- Nom du fichier sans extension
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    checksum VARCHAR(32) NOT NULL       -- MD5 du contenu du script
);

CREATE INDEX IF NOT EXISTS idx_schema_migrations_applied_at
ON schema_migrations(applied_at);
```

### Idempotence

- **Script déjà appliqué** → skippé automatiquement
- **Checksum modifié** → avertissement dans les logs (script déjà appliqué avec contenu différent)
- Chaque script s'exécute dans sa propre **transaction** (rollback automatique si échec)

## Architecture

```
Program.Main(args)
    │
    ├── CliArgumentParser.Parse(args) → CliOptions
    │       └── Validation: options mutuellement exclusives
    │
    ├── Host.CreateDefaultBuilder()
    │       └── DI: IConsoleLogger, IMigrationRepository, ConfigurationManager, IMigrationEngine
    │
    ├── ConfigurationManager.LoadConfiguration()
    │       └── Priorité: CLI > ENV > appsettings.json
    │
    ├── ConfigurationManager.TestConnection()
    │       └── SELECT version() — masque mot de passe dans les logs
    │
    └── Route execution:
        ├── --query mode → QueryExecutor.ExecuteQueryAsync()
        │       └── Résultats en tableau ASCII formaté
        └── Migration mode → IMigrationEngine.ExecuteAsync()
                └── schema_migrations tracking (version, applied_at, checksum MD5)
                └── Transaction par script (rollback automatique si échec)
                └── Idempotent: skip si déjà appliqué
```

## Notes

- Projet **standalone** — aucune dépendance vers Domain, Application, Infrastructure
- `ManagePackageVersionsCentrally=false` dans le `.csproj` pour compatibilité submodule (évite les conflits CPM)
- Connection string masquée dans les logs (mot de passe remplacé par `****`)
- Logger console thread-safe avec couleurs (vert=succès, rouge=erreur, jaune=skip/warning)
