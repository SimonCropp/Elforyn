# EntityFramework Usage

Interactions with PostgreSQL via [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).


## Elforyn package [![NuGet Status](https://img.shields.io/nuget/v/Elforyn.svg)](https://www.nuget.org/packages/Elforyn/)

https://nuget.org/packages/Elforyn/


## Schema and data

The snippets use a DbContext of the following form:

snippet: Elforyn.Tests/TestDbContext.cs


## Initialize PgInstance

PgInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the PgInstance existing in the same test class, then the PgInstance can be initialized in the static constructor of that test class.

snippet: PgStaticConstructor


### Static constructor in test base

If multiple tests need to use the PgInstance, then the PgInstance should be initialized in the static constructor of test base class.

snippet: PgTestBase


### NpgsqlDbContextOptionsBuilder

Some Npgsql options are exposed by passing a `Action<NpgsqlDbContextOptionsBuilder>` to `NpgsqlDbContextOptionsExtensions.UseNpgsql`. In this project the `UseNpgsql` is handled internally, so the NpgsqlDbContextOptionsBuilder functionality is achieved by passing an action to the PgInstance.

snippet: npgsqlOptionsBuilder


### Seeding data in the template

Data can be seeded into the template database for use across all tests:

snippet: PgBuildTemplate


## Usage in a Test

Usage inside a test consists of two parts:


### Build a PgDatabase

snippet: PgBuildDatabase


### Using DbContexts

snippet: PgBuildContext


#### Full Test

The above are combined in a full test:

snippet: PgSnippetTests


## Shared Database

`BuildShared` creates a single database from the template once and reuses it across calls. This is useful for query-only tests that don't need per-test isolation.

snippet: PgSharedDatabase

Pass `useTransaction: true` to get an auto-rolling-back transaction, allowing writes without affecting other tests.

Note: `useTransaction: true` means that on test failure the resulting database cannot be inspected (since the transaction is rolled back). A workaround when debugging a failure is to temporarily remove `useTransaction: true`.

snippet: PgSharedDatabase_WithTransaction


### EntityFramework DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

snippet: Elforyn/DefaultOptionsBuilder.cs