# CSV to Table

A tool for importing CSV data directly into a newly created SQL table. CSV file name becomes table name. Column names become field names. The tool also has opinions on how to handle PK and create/modified columns:

- Id is always the PK and of type uniqueidentifier
- DateCreated is datetime2 and default to GETUTSYSCDATE()
- DateModified is the same, but nullable
- UserCreatedId and UserModifiedId are uniqueidentifier, one nullable one not

All data is considered nvarchar (255) with the intention to change the type once the data is on server.

You can for example use this to fix the data for conversion to decimal:

```sql
UPDATE dbo.Table
SET [Column with decimal data] = REPLACE([Column with decimal data], ',', '.')
WHERE [Column with decimal data] LIKE '%,%';
```

## Built with

Our dear friend and future overlord, Chat GPT-5 mini.