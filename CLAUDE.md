# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

Elforyn is a .NET library wrapping PostgreSQL for isolated test databases using EF Core. It creates a template database once, then clones it per test using `CREATE DATABASE ... TEMPLATE` for speed. Test framework adapters are provided for NUnit, xUnit v3, MSTest, and TUnit.

## Build & Test Commands

```bash
# Build
dotnet build src --configuration Release

# Run all tests except TUnit
dotnet test src/Elforyn.Tests --configuration Release

# TUnit tests MUST use dotnet run (dotnet test fails on .NET 10)
dotnet run --project src/Elforyn.TUnit.Tests --configuration Release

# Run a single test (non-TUnit)
dotnet test src/Elforyn.Tests --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

### Design

- **`src/Elforyn/`** - Core EF Core layer. `Wrapper` manages PostgreSQL template database lifecycle via `CREATE DATABASE ... TEMPLATE`. `PgInstance<TDbContext>` creates/clones template databases. `PgDatabase<TDbContext>` represents a single test database with `NpgsqlConnection`, `Context` (tracking) and `NoTrackingContext` properties.

### Test Framework Adapters

Each adapter (`Elforyn.NUnit`, `Elforyn.Xunit.V3`, `Elforyn.MSTest`, `Elforyn.TUnit`) provides a `PgTestBase<TDbContext>` base class with lifecycle management, AAA phase enforcement (`ArrangeData`/`ActData`/`AssertData`), and `[SharedDb]`/`[SharedDbWithTransaction]` attributes.

**Shared code pattern:** Common files live in `Elforyn.MSTest/` and are file-linked into NUnit, xUnit v3, and TUnit projects via `<Compile Include="..\Elforyn.MSTest\..." Link="..." />`. Only `PgTestBase.cs` differs per framework.

### Key Classes

- `Wrapper` (`Elforyn/Wrapper.cs`) - PostgreSQL template database lifecycle using `CREATE DATABASE ... TEMPLATE`, timestamp via `COMMENT ON DATABASE`
- `PgInstance<TDbContext>` - Template creation, database cloning, timestamp-based invalidation
- `PgDatabase<TDbContext>` - Per-test database, disposal, connection management
- `PgTestBase<TDbContext>` - Framework-specific test base class

## Important Conventions

- **Strong-named assemblies** using `src/key.snk`. `InternalsVisibleTo` requires full public key (see `src/Elforyn/InternalsVisibleTo.cs`).
- **Central package management** via `src/Directory.Packages.props`.
- **TreatWarningsAsErrors** is enabled. LangVersion is `preview`.
- **Verify** snapshot testing: `.verified.txt` files are checked in. On failure, `.received.txt` is generated; copy it over `.verified.txt` to accept.
- **Solution file:** `src/Elforyn.slnx`.

## Framework-Specific Gotchas

- **xUnit v3:** Uses stdout for JSON protocol. Wrap `Initialize()` with `Console.SetOut(TextWriter.Null)`.
- **MSTest:** `[UsesVerify]` source generator creates `TestContext` property - don't add one manually.
- **TUnit:** AsyncLocal state must be set in `[Before(HookType.Test)]`, not constructor. Call `context.AddAsyncLocalValues()` after. `Assembly` name conflicts with `HookType.Assembly` - use `System.Reflection.Assembly`.
- **BuildVerifier param order:** xUnit v3/MSTest use `(settings, sourceFile)`, NUnit/TUnit use `(sourceFile, settings)`.
