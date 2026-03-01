# Template Regen

Template re-generating performs the following action

 * Drop the existing template database.
 * Create a new template database.
 * Apply the default schema and data as defined by the `buildTemplate` parameter in the `PgInstance` constructor.

Re-generating the template database is a relatively expensive operation. Ideally, it should only be performed when the default schema and data requires a change. To enable this a timestamp convention is used. When the template is re-generated, a timestamp is stored via `COMMENT ON DATABASE`. On the next run, that timestamp is compared to avoid re-generation on a match. The timestamp can be controlled via `timestamp` parameter in the `PgInstance` constructor.

If `timestamp` parameter is not defined the following convention is used:

 * The last modified time of the Assembly containing the DbContext.

There is a timestamp helper class to help derive last modified time of an Assembly (if the above convention does not suffice):

snippet: Timestamp