# EntityFramework MSTest Usage

Combines [Elforyn](/pages/ef-usage.md), [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro), [Verify.MSTest](https://github.com/VerifyTests/Verify#verifymstest), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics#write-your-tests) phase enforcement.


## Elforyn.MSTest package [![NuGet Status](https://img.shields.io/nuget/v/Elforyn.MSTest.svg)](https://www.nuget.org/packages/Elforyn.MSTest/)

https://nuget.org/packages/Elforyn.MSTest/


## Schema and data

The snippets use a DbContext of the following form:

snippet: Elforyn.MSTest.Tests/Model/TheDbContext.cs

snippet: Elforyn.MSTest.Tests/Model/Company.cs

snippet: Elforyn.MSTest.Tests/Model/Employee.cs


## Initialize

`PgTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers):

snippet: Elforyn.MSTest.Tests/ModuleInitializer.cs


## Usage in a Test

Inherit from `PgTestBase<T>` and use the `ArrangeData`, `ActData`, and `AssertData` properties. These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception.

snippet: Elforyn.MSTest.Tests/Tests.cs


## Static Instance

The current test instance can be accessed via `PgTestBase<T>.Instance`. This is useful when test helpers need to access the database outside the test class:

snippet: StaticInstanceMSTest


## Combinations

[Verify Combinations](https://github.com/VerifyTests/Verify#combinations) are supported. The database is reset for each combination:

snippet: CombinationsMSTest


## VerifyEntity

Helpers for verifying entities by primary key, with optional Include/ThenInclude:

snippet: VerifyEntityMSTest

snippet: VerifyEntityWithIncludeMSTest

snippet: VerifyEntityWithThenIncludeMSTest


## VerifyEntities

Verify a collection of entities from a `DbSet` or `IQueryable`:

snippet: VerifyEntities_DbSetMSTest

snippet: VerifyEntity_QueryableMSTest


## SharedDb

include: shared-db

snippet: SharedDbTestsMSTest


## Parallel Execution

To run tests in parallel, configure parallelism at the assembly level:

snippet: Elforyn.MSTest.Tests/TestConfig.cs