using System.Reflection;
using System.Text;
using CommandLine;
using Csv2Table;

internal static class Program
{
    [STAThread] // for WinForms compatibility
    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            var assembly = typeof(Program).Assembly;
            var assemblyVersion = assembly.GetName().Version?.ToString() ?? "?";
            var fileVersionAttr = (AssemblyFileVersionAttribute)(
                Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute))
                ?? throw new InvalidOperationException("AssemblyFileVersionAttribute not found")
            );
            Console.WriteLine($"Version: {assemblyVersion} / {fileVersionAttr?.Version ?? "?"}");

            var parser = new Parser(with =>
            {
                with.HelpWriter = Console.Out;
                with.EnableDashDash = true;
            });

            var result = parser.ParseArguments<Table2CsvOptions>(args);
            var exitCode = result.MapResult(
                (Table2CsvOptions opts) =>
                {
                    var filePath = opts.FilePath;
                    if (!File.Exists(filePath))
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Title = "Select a file";
                        openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                        openFileDialog.ShowDialog();
                        if (!File.Exists(openFileDialog.FileName))
                        {
                            Console.WriteLine("No file selected or found.");
                            return -5;
                        }
                        filePath = openFileDialog.FileName;
                    }
                    opts.FilePath = filePath;

                    var tableName =
                        Path.GetFileNameWithoutExtension(filePath)
                        ?? throw new ArgumentException("Could not determine table name.");
                    var r1 = OperationsCsv.LoadCsv(opts, tableName, out var rows, out var sql);
                    if (r1 < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error loading CSV file.");
                        Console.ResetColor();
                        return -10;
                    }
                    var r2 = OperationsSql.CreateAndLoadTable(opts, tableName, rows, sql);
                    if (r2 < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error creating/loading table in database.");
                        Console.ResetColor();
                        return -20;
                    }
                    return 0;
                },
                errs => 1
            );
            Console.ForegroundColor = exitCode < 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"Exit code: {exitCode}");
            Console.ResetColor();
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
            Console.WriteLine(ex.StackTrace);
            return -99;
        }
    }
}
