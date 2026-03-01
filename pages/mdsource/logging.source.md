# Logging

By default some information is written to [Trace.WriteLine](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.writeline#System_Diagnostics_Trace_WriteLine_System_String_System_String_)

 * The PostgreSQL connection when `PgInstance` is instantiated.
 * The database name when a `PgDatabase` is built.

To enable verbose logging use `ElforynLogging`:

snippet: ElforynLoggingUsage

The full implementation is:

snippet: ElforynLogging

Which is then combined with [Fody MethodTimer](https://github.com/Fody/MethodTimer):

snippet: MethodTimeLogger


## SQL statements

SQL statements can be logged:

snippet: ElforynLoggingUsageSqlLogging

This will also log EntityFramework SQL statements.

So performing a `DbSet.FindAsync()` would result in logged EF SQL commands.