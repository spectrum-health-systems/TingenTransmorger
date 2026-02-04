using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using TingenTransmorger.Core;

namespace TingenTransmorger.TeleHealthReport
{
    class MessageFailureReport
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

            // Summary aggregation: key/value pairs with summed values
            var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            (string, string)? summaryHeaders = null;

            // SMS Stats aggregation: keyed by Client Name, storing all records
            var smsStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
            var smsStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Email Stats aggregation: keyed by Client Name, storing all records
            var emailStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
            var emailStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var files = Directory.GetFiles(importDir, "*Message_Failure*.xlsx", SearchOption.TopDirectoryOnly);

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

                    // Process Message Delivery Summary sheet
                    if (sheetName.Equals("Message Delivery Summary", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessSummarySheet(table, summaryMetrics, ref summaryHeaders);
                    }
                    // Process SMS STATS sheet
                    else if (sheetName.Equals("SMS STATS", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessSmsStatsSheet(table, smsStatsByClient, smsStatsHeaders);
                    }
                    // Process EMAIL STATS sheet
                    else if (sheetName.Equals("EMAIL STATS", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessEmailStatsSheet(table, emailStatsByClient, emailStatsHeaders);
                    }
                }
            }

            // Write Summary JSON
            WriteSummaryJson(tmpDir, summaryMetrics, summaryHeaders);

            // Write SMS Stats JSON
            WriteSmsStatsJson(tmpDir, smsStatsByClient);

            // Write Email Stats JSON
            WriteEmailStatsJson(tmpDir, emailStatsByClient);
        }

        private static void ProcessSummarySheet(System.Data.DataTable table, Dictionary<string, double> summaryMetrics, ref (string, string)? summaryHeaders)
        {
            if (table.Columns.Count < 2)
                return;

            // Capture headers once
            summaryHeaders ??= (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");

            foreach (System.Data.DataRow dr in table.Rows)
            {
                var key = dr[0]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                // Try parse numeric value with efficient fallback
                var value = ParseDoubleValue(dr[1]);

                if (summaryMetrics.TryGetValue(key, out var existing))
                    summaryMetrics[key] = existing + value;
                else
                    summaryMetrics[key] = value;
            }
        }

        private static void ProcessSmsStatsSheet(System.Data.DataTable table, Dictionary<string, List<Dictionary<string, object?>>> smsStatsByClient, HashSet<string> smsStatsHeaders)
        {
            if (table.Columns.Count == 0)
                return;

            // Check if Client Name column exists in current table
            if (!table.Columns.Contains("Client Name"))
                return;

            // Build column name cache and update headers set
            var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Data.DataColumn col in table.Columns)
            {
                var colName = col.ColumnName ?? string.Empty;
                tableColumns.Add(colName);
                smsStatsHeaders.Add(colName);
            }

            // Get ordered list of headers for consistent output
            var orderedHeaders = smsStatsHeaders.ToList();

            foreach (System.Data.DataRow dr in table.Rows)
            {
                var clientName = dr["Client Name"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(clientName))
                    continue;

                // Build row dictionary
                var row = new Dictionary<string, object?>(orderedHeaders.Count);
                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dr[header] : null;
                }

                // Add record to client's list (allowing duplicates)
                if (!smsStatsByClient.TryGetValue(clientName, out var clientRecords))
                {
                    clientRecords = new List<Dictionary<string, object?>>();
                    smsStatsByClient[clientName] = clientRecords;
                }
                clientRecords.Add(row);
            }
        }

        private static void ProcessEmailStatsSheet(System.Data.DataTable table, Dictionary<string, List<Dictionary<string, object?>>> emailStatsByClient, HashSet<string> emailStatsHeaders)
        {
            if (table.Columns.Count == 0)
                return;

            // Check if Client Name column exists in current table
            if (!table.Columns.Contains("Client Name"))
                return;

            // Build column name cache and update headers set
            var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Data.DataColumn col in table.Columns)
            {
                var colName = col.ColumnName ?? string.Empty;
                tableColumns.Add(colName);
                emailStatsHeaders.Add(colName);
            }

            // Get ordered list of headers for consistent output
            var orderedHeaders = emailStatsHeaders.ToList();

            foreach (System.Data.DataRow dr in table.Rows)
            {
                var clientName = dr["Client Name"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(clientName))
                    continue;

                // Build row dictionary
                var row = new Dictionary<string, object?>(orderedHeaders.Count);
                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dr[header] : null;
                }

                // Add record to client's list (allowing duplicates)
                if (!emailStatsByClient.TryGetValue(clientName, out var clientRecords))
                {
                    clientRecords = new List<Dictionary<string, object?>>();
                    emailStatsByClient[clientName] = clientRecords;
                }
                clientRecords.Add(row);
            }
        }

        private static void WriteSummaryJson(string tmpDir, Dictionary<string, double> summaryMetrics, (string, string)? summaryHeaders)
        {
            if (summaryMetrics.Count == 0 || summaryHeaders == null)
                return;

            var summaryRows = summaryMetrics.Select(kv => new Dictionary<string, object?>
            {
                [summaryHeaders.Value.Item1] = kv.Key,
                [summaryHeaders.Value.Item2] = kv.Value
            }).ToList();

            var summaryPath = Path.Combine(tmpDir, "Message_Failure-Summary.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var summaryJson = JsonSerializer.Serialize(summaryRows, options);
            File.WriteAllText(summaryPath, summaryJson, Encoding.UTF8);
        }

        private static void WriteSmsStatsJson(string tmpDir, Dictionary<string, List<Dictionary<string, object?>>> smsStatsByClient)
        {
            if (smsStatsByClient.Count == 0)
                return;

            // Group structure: each client has their records array
            var smsStatsGrouped = new List<Dictionary<string, object?>>();
            foreach (var kvp in smsStatsByClient)
            {
                var clientName = kvp.Key;
                var records = kvp.Value;

                // Deduplicate exact duplicate records
                var uniqueRecords = DeduplicateRecords(records);

                var clientGroup = new Dictionary<string, object?>
                {
                    ["Client Name"] = clientName,
                    ["Records"] = uniqueRecords
                };
                smsStatsGrouped.Add(clientGroup);
            }

            var smsStatsPath = Path.Combine(tmpDir, "Message_Failure-SMS_Stats.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var smsStatsJson = JsonSerializer.Serialize(smsStatsGrouped, options);
            File.WriteAllText(smsStatsPath, smsStatsJson, Encoding.UTF8);
        }

        private static void WriteEmailStatsJson(string tmpDir, Dictionary<string, List<Dictionary<string, object?>>> emailStatsByClient)
        {
            if (emailStatsByClient.Count == 0)
                return;

            // Group structure: each client has their records array
            var emailStatsGrouped = new List<Dictionary<string, object?>>();
            foreach (var kvp in emailStatsByClient)
            {
                var clientName = kvp.Key;
                var records = kvp.Value;

                // Deduplicate exact duplicate records
                var uniqueRecords = DeduplicateRecords(records);

                var clientGroup = new Dictionary<string, object?>
                {
                    ["Client Name"] = clientName,
                    ["Records"] = uniqueRecords
                };
                emailStatsGrouped.Add(clientGroup);
            }

            var emailStatsPath = Path.Combine(tmpDir, "Message_Failure-Email_Stats.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var emailStatsJson = JsonSerializer.Serialize(emailStatsGrouped, options);
            File.WriteAllText(emailStatsPath, emailStatsJson, Encoding.UTF8);
        }

        private static double ParseDoubleValue(object value)
        {
            if (value == null)
                return 0;

            // Attempt to parse directly
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            // Fallback to parsing as text (remove quotes)
            var stringValue = value.ToString()?.Trim('"');
            return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0;
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
