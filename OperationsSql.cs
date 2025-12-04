using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Csv2Table
{
    internal class OperationsSql
    {
        public static int CreateAndLoadTable(Table2CsvOptions opts, string tableName, List<dynamic> rows, string sql)
        {
            if (opts == null)
                throw new ArgumentNullException(nameof(opts));
            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                throw new ArgumentException("Connection string is required on opts.", nameof(opts));
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("Create table SQL required.", nameof(sql));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException(nameof(tableName));

            Console.WriteLine("Creating and loading table into the database...");

            var first = rows[0];
            List<string> headers = first is IDictionary<string, object> dictFirst
                ? dictFirst.Keys.ToList()
                : throw new ArgumentException("Rows must be IDictionary<string, object>-backed dynamic objects.");

            var insertColumns = headers.ToList();
            try
            {
                using var conn = new SqlConnection(opts.ConnectionString);
                conn.Open();

                using var tran = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.Parameters.Clear();
                cmd.ExecuteNonQuery();

                insertColumns.Add("UserCreatedId");
                var columnList = string.Join(", ", insertColumns.Select(OperationsCsv.EscapeIdentifier));
                var paramNamesTemplate = string.Join(", ", Enumerable.Range(0, insertColumns.Count).Select(i => $"@p{i}"));
                var insertSql = $"INSERT INTO {tableName} ({columnList}) VALUES ({paramNamesTemplate})";

                foreach (var row in rows)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = insertSql;

                    var dict = row as IDictionary<string, object>;
                    int i;
                    for (i = 0; i < insertColumns.Count - 1; i++)
                    {
                        object? value = null;
                        var col = insertColumns[i];
                        if (dict != null)
                        {
                            dict.TryGetValue(col, out value);
                        }
                        else
                        {
                            var prop = row.GetType().GetProperty(col);
                            if (prop != null)
                                value = prop.GetValue(row);
                        }

                        var param = cmd.CreateParameter();
                        param.ParameterName = $"@p{i}";
                        param.Value = value ?? DBNull.Value;
                        cmd.Parameters.Add(param);
                    }
                    var pm = cmd.CreateParameter();
                    pm.ParameterName = $"@p{i}";
                    pm.Value = opts.UserId;
                    cmd.Parameters.Add(pm);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                Console.WriteLine("Table created and rows inserted successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error creating/loading table: " + ex.Message);
                Console.ResetColor();
                Console.WriteLine(ex.StackTrace);
                return -1;
            }
        }
    }
}
