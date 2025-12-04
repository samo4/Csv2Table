using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration;

namespace Csv2Table
{
    internal class OperationsCsv
    {
        public static int LoadCsv(Table2CsvOptions opts, string tableName, out List<dynamic> rows, out string sql)
        {
            var filePath = opts.FilePath;
            rows = LoadCsv(filePath, print: true);
            sql = PrepareCreateTableStatement(tableName, rows);
            Console.WriteLine("Generated SQL CREATE TABLE statement:");
            Console.WriteLine(sql);
            return 0;
        }

        public static string PrepareCreateTableStatement(string tableName, List<dynamic> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                throw new ArgumentException("No rows to infer schema from.");
            }

            var first = rows[0];
            List<string> headers = first is IDictionary<string, object> dictFirst
                ? dictFirst.Keys.ToList()
                : throw new ArgumentException("no headers");

            var columns = new List<string>();
            columns.Add($"    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY");
            foreach (var header in headers)
            {
                var col = $"    {EscapeIdentifier(header)} VARCHAR(255) NOT NULL";
                columns.Add(col);
            }
            columns.Add("    [DateCreated] [datetime2](7) NOT NULL DEFAULT SYSUTCDATETIME() ");
            columns.Add("    [DateModified] [datetime2](7) NULL ");
            columns.Add("    [UserCreatedId] UNIQUEIDENTIFIER NOT NULL ");
            columns.Add("    [UserModifiedId] UNIQUEIDENTIFIER NULL ");

            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {EscapeIdentifier(tableName)} (");
            sb.AppendLine(string.Join("," + Environment.NewLine, columns));
            sb.AppendLine(");");
            return sb.ToString();
        }

        public static List<dynamic> LoadCsv(string filePath, bool print = false, string separator = "")
        {
            string delimiter = separator;
            if (delimiter.Equals("tab", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = "\t";
            }
            else if (delimiter.Equals("space", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = " ";
            }
            else
            {
                var firstLine = File.ReadLines(filePath).FirstOrDefault() ?? string.Empty;
                delimiter = DetectSeparatorFromFirstLine(firstLine) ?? ",";
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<dynamic>();
                var result = records.ToList();
                if (print)
                {
                    Print(result);
                }
                return result;
            }
        }

        public static string EscapeIdentifier(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            return $"[{identifier.Replace("]", "]]")}]";
        }

        private static string? DetectSeparatorFromFirstLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return ",";

            var candidates = new[] { ",", ";", "|" };

            foreach (var cand in candidates)
            {
                if (line.Contains(cand))
                    return cand;
            }

            return ",";
        }

        public static void Print(List<dynamic> rows, int maxRowsToShow = 20)
        {
            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("No rows to display.");
                return;
            }

            var first = rows[0];
            List<string> headers = first is IDictionary<string, object> dictFirst
                ? dictFirst.Keys.ToList()
                : throw new Exception("no headers");

            var table = new List<string[]>();
            foreach (var row in rows)
            {
                string[] cells = new string[headers.Count];
                var dict = row as IDictionary<string, object>;
                for (int i = 0; i < headers.Count; i++)
                {
                    object? value = null;
                    if (dict != null)
                    {
                        dict.TryGetValue(headers[i], out value);
                    }
                    else
                    {
                        var prop = row.GetType().GetProperty(headers[i]);
                        if (prop != null)
                        {
                            value = prop.GetValue(row);
                        }
                    }

                    var text = value?.ToString() ?? string.Empty;
                    text = text.Replace("\r", " ").Replace("\n", " ");
                    cells[i] = text;
                }
                table.Add(cells);
            }

            const int maxColWidth = 40;
            int colCount = headers.Count;
            int[] colWidths = new int[colCount];
            for (int c = 0; c < colCount; c++)
            {
                int max = headers[c].Length;
                foreach (var row in table)
                {
                    if (row[c] != null)
                    {
                        var len = row[c].Length;
                        if (len > max)
                            max = len;
                    }
                }
                colWidths[c] = Math.Min(max, maxColWidth);
            }

            string Truncate(string s, int width)
            {
                if (s.Length <= width)
                    return s;
                if (width <= 3)
                    return s.Substring(0, width);
                return s.Substring(0, width - 3) + "...";
            }

            var headerSb = new StringBuilder();
            headerSb.Append("|");
            for (int c = 0; c < colCount; c++)
            {
                var h = Truncate(headers[c], colWidths[c]);
                headerSb.Append(" " + h.PadRight(colWidths[c]) + " |");
            }
            Console.WriteLine(headerSb.ToString());

            var sepSb = new StringBuilder();
            sepSb.Append("|");
            for (int c = 0; c < colCount; c++)
            {
                sepSb.Append(" " + new string('-', colWidths[c]) + " |");
            }
            Console.WriteLine(sepSb.ToString());

            int rowsShown = Math.Min(rows.Count, maxRowsToShow);
            for (int r = 0; r < rowsShown; r++)
            {
                var row = table[r];
                var rowSb = new StringBuilder();
                rowSb.Append("|");
                for (int c = 0; c < colCount; c++)
                {
                    var cell = Truncate(row[c] ?? string.Empty, colWidths[c]);
                    rowSb.Append(" " + cell.PadRight(colWidths[c]) + " |");
                }
                Console.WriteLine(rowSb.ToString());
            }

            if (rows.Count > maxRowsToShow)
            {
                Console.WriteLine($"... ({rows.Count - maxRowsToShow} more rows not shown)");
            }
        }
    }
}
