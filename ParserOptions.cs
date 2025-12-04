using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

[Verb("createandload", HelpText = "Creates a table and loads the CSV into it.")]
class Table2CsvOptions
{
    [Option('f', "file", HelpText = "CSV file")]
    public string FilePath { get; set; } = string.Empty;

    [Option('c', "connectionString", HelpText = "Database connection string with create table")]
    public string ConnectionString { get; set; } = string.Empty;

    [Option('u', "user", HelpText = "User ID")]
    public string UserId { get; set; } = Guid.NewGuid().ToString();
}
