using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using TingenTransmorger.Core;

namespace TingenTransmorger.TeleHealthReport
{
    class MessageDeliveryReport
    {
        internal static void Adder(Configuration config)
        {
            if (config == null)
                return;

            // Register encoding provider required by ExcelDataReader
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!config.AdminDirectories.TryGetValue("Import", out var importDir) || string.IsNullOrEmpty(importDir))
            {
                return; // nothing to do
            }

            if (!config.AdminDirectories.TryGetValue("Tmp", out var tmpDir) || string.IsNullOrEmpty(tmpDir))
            {
                return; // no tmp location
            }

            if (!Directory.Exists(importDir))
                return;

            Directory.CreateDirectory(tmpDir);

            // Collect all records from all files
            var allRecords = new List<Dictionary<string, object?>>();
            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var files = Directory.GetFiles(importDir, "*Message_Delivery*.xlsx", SearchOption.TopDirectoryOnly);

            var excelConfig = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };

            foreach (var file in files)
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet(excelConfig);

                foreach (System.Data.DataTable table in dataSet.Tables)
                {
                    if (table.Columns.Count == 0)
                        continue;

                    var sheetName = table.TableName;

                    // Process Message Delivery Stats sheet
                    if (sheetName.Equals("Message Delivery Stats", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessMessageDeliveryStatsSheet(table, allRecords, headers);
                    }
                }
            }

            // Write combined JSON
            WriteMessageDeliveryStatsJson(tmpDir, allRecords);
        }

        private static void ProcessMessageDeliveryStatsSheet(System.Data.DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
        {
            if (table.Columns.Count == 0)
                return;

            // Build column name cache and update headers set
            var tableColumns = new List<string>();
            foreach (System.Data.DataColumn col in table.Columns)
            {
                var colName = col.ColumnName ?? string.Empty;
                tableColumns.Add(colName);
                headers.Add(colName);
            }

            // Get ordered list of headers for consistent output
            var orderedHeaders = headers.ToList();

            foreach (System.Data.DataRow dr in table.Rows)
            {
                // Build row dictionary - include all columns
                var row = new Dictionary<string, object?>(orderedHeaders.Count);
                
                for (int i = 0; i < tableColumns.Count; i++)
                {
                    var colName = tableColumns[i];
                    row[colName] = dr[i];
                }

                // Add all remaining headers that might not be in this table
                foreach (var header in orderedHeaders)
                {
                    if (!row.ContainsKey(header))
                    {
                        row[header] = null;
                    }
                }

                // Add record (allowing duplicates)
                allRecords.Add(row);
            }
        }

        private static void WriteMessageDeliveryStatsJson(string tmpDir, List<Dictionary<string, object?>> allRecords)
        {
            if (allRecords.Count == 0)
                return;

            // Deduplicate exact duplicate records
            var uniqueRecords = DeduplicateRecords(allRecords);

            var outputPath = Path.Combine(tmpDir, "Message_Delivery-Message_Delivery_Stats.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(uniqueRecords, options);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }

        private static List<Dictionary<string, object?>> DeduplicateRecords(List<Dictionary<string, object?>> records)
        {
            var unique = new HashSet<string>();
            var result = new List<Dictionary<string, object?>>();

            foreach (var record in records)
            {
                // Serialize each record to JSON for comparison
                var json = JsonSerializer.Serialize(record);
                if (unique.Add(json))
                {
                    result.Add(record);
                }
            }
            return result;
        }
    }
}
