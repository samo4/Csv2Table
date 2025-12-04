# CSV to Table

A small tool to import CSV data into a newly created SQL Server table.

Basic behaviour
- Table name is inferred from the CSV file name (extension removed).
- Every CSV column becomes `NVARCHAR(255)` by default.
- If the CSV does not contain an `Id` column (case-insensitive) the tool will add:
  - `Id` UNIQUEIDENTIFIER NOT NULL DEFAULT `NEWID()` PRIMARY KEY
- Audit columns are added when appropriate:
  - `DateCreated` DATETIME2 NOT NULL DEFAULT `SYSUTCDATETIME()`
  - `DateModified` DATETIME2 NULL
  - `UserCreatedId` UNIQUEIDENTIFIER NOT NULL
  - `UserModifiedId` UNIQUEIDENTIFIER NULL
- Identifiers are escaped with square brackets for SQL Server compatibility.

Delimiter detection is guessing; you can pass a delimiter explicitly if needed. The tool executes the generated `CREATE TABLE` SQL and inserts rows using parameterized statements inside a transaction.

Windows only (for now) (I was lazy and wanted to use system file dialogs).

## Getting started

- Build with .NET 10 SDK.
- Provide a valid connection string to load the created table into your SQL Server instance.

## Usage

You can for example use this to fix the data for conversion to decimal:

```sql
UPDATE dbo.Table
SET [Column with decimal data] = REPLACE([Column with decimal data], ',', '.')
WHERE [Column with decimal data] LIKE '%,%';
```

## Built with

Our dear friend and future overlord, Chat GPT-5 mini.