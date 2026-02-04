// 260204_code
// 260204_documentation

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using TingenTransmorger.Core;

namespace TingenTransmorger.TeleHealthReport;

class TeleHealthReportProcessor
{
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static void Process(Configuration config)
    {
        //if (config == null ||
        //    !config.AdminDirectories.TryGetValue("Import", out var importDir) ||
        //    !config.AdminDirectories.TryGetValue("Tmp", out var tmpDir) ||
        //    !Directory.Exists(importDir))
        //    return;

        var importDir = config.AdminDirectories["Import"];
        var tmpDir = config.AdminDirectories["Tmp"];

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);

        ProcessVisitStatsReports(importDir, tmpDir);
        ProcessVisitDetailsReports(importDir, tmpDir);
        ProcessMessageFailureReports(importDir, tmpDir);
        ProcessMessageDeliveryReports(importDir, tmpDir);
    }

    private static void ProcessVisitStatsReports(string importDir, string tmpDir)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        (string, string)? summaryHeaders = null;
        var meetingErrorsById = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingErrorHeaders = new List<string>();

        ProcessExcelFiles(importDir, "*Visit_Stats*.xlsx", (table, sheetName) =>
        {
            if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSummarySheet(table, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("Meeting Errors", StringComparison.OrdinalIgnoreCase))
            {
                ProcessKeyedSheet(table, meetingErrorsById, meetingErrorHeaders, "Meeting ID", aggregateNumeric: true);
            }
        });

        WriteSummaryJson(tmpDir, "Visit_Stats-Summary.json", summaryMetrics, summaryHeaders);
        WriteKeyedJson(tmpDir, "Visit_Stats-Meeting_Errors.json", meetingErrorsById);
    }

    private static void ProcessVisitDetailsReports(string importDir, string tmpDir)
    {
        var meetingDetailsById = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var participantDetailsByName = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ProcessExcelFiles(importDir, "*Visit_Details*.xlsx", (table, sheetName) =>
        {
            if (sheetName.Equals("Meeting Details", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSimpleKeyedSheet(table, meetingDetailsById, meetingDetailsHeaders, "Meeting ID");
            }
            else if (sheetName.Equals("Participant Details", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSimpleKeyedSheet(table, participantDetailsByName, participantDetailsHeaders, "Participant Name");
            }
        });

        WriteSimpleJson(tmpDir, "Visit_Details-Meeting_Details.json", meetingDetailsById);
        WriteSimpleJson(tmpDir, "Visit_Details-Participant_Details.json", participantDetailsByName);
    }

    private static void ProcessMessageFailureReports(string importDir, string tmpDir)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        (string, string)? summaryHeaders = null;
        var smsStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var smsStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var emailStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var emailStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ProcessExcelFiles(importDir, "*Message_Failure*.xlsx", (table, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSummarySheet(table, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("SMS STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessClientStatsSheet(table, smsStatsByClient, smsStatsHeaders);
            }
            else if (sheetName.Equals("EMAIL STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessClientStatsSheet(table, emailStatsByClient, emailStatsHeaders);
            }
        });

        WriteSummaryJson(tmpDir, "Message_Failure-Summary.json", summaryMetrics, summaryHeaders);
        WriteClientStatsJson(tmpDir, "Message_Failure-SMS_Stats.json", smsStatsByClient);
        WriteClientStatsJson(tmpDir, "Message_Failure-Email_Stats.json", emailStatsByClient);
    }

    private static void ProcessMessageDeliveryReports(string importDir, string tmpDir)
    {
        var allRecords = new List<Dictionary<string, object?>>();
        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ProcessExcelFiles(importDir, "*Message_Delivery*.xlsx", (table, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Stats", StringComparison.OrdinalIgnoreCase))
            {
                ProcessFlatSheet(table, allRecords, headers);
            }
        });

        WriteFlatJson(tmpDir, "Message_Delivery-Message_Delivery_Stats.json", allRecords);
    }

    private static void ProcessExcelFiles(string importDir, string pattern, Action<System.Data.DataTable, string> processSheet)
    {
        foreach (var file in Directory.GetFiles(importDir, pattern, SearchOption.TopDirectoryOnly))
        {
            using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(ExcelConfig);

            foreach (System.Data.DataTable table in dataSet.Tables)
            {
                if (table.Columns.Count > 0)
                {
                    processSheet(table, table.TableName);
                }
            }
        }
    }

    private static void ProcessSummarySheet(System.Data.DataTable table, Dictionary<string, double> metrics, ref (string, string)? headers)
    {
        if (table.Columns.Count < 2)
            return;

        headers ??= (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var key = dr[0]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var value = ParseDoubleValue(dr[1]);
            metrics[key] = metrics.TryGetValue(key, out var existing) ? existing + value : value;
        }
    }

    private static void ProcessKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById,
        List<string> headers, string keyColumn, bool aggregateNumeric = false)
    {
        if (!table.Columns.Contains(keyColumn))
            return;

        UpdateHeaders(table, headers);

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var key = dr[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var row = BuildRow(table, headers);

            foreach (var header in headers)
            {
                row[header] = table.Columns.Contains(header) ? dr[header] : null;
            }

            if (dataById.TryGetValue(key, out var existingRow) && aggregateNumeric)
            {
                MergeRows(existingRow, row, keyColumn);
            }
            else
            {
                dataById.TryAdd(key, row);
            }
        }
    }

    private static void ProcessSimpleKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, HashSet<string> headers, string keyColumn)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        var tableColumns = UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var key = dr[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!dataById.ContainsKey(key))
            {
                var row = new Dictionary<string, object?>(orderedHeaders.Count);

                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dr[header] : null;
                }

                dataById[key] = row;
            }
        }
    }

    private static void ProcessClientStatsSheet(System.Data.DataTable table, Dictionary<string, List<Dictionary<string, object?>>> statsByClient, HashSet<string> headers)
    {
        if (!table.Columns.Contains("Client Name"))
        {
            return;
        }

        var tableColumns = UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var clientName = dr["Client Name"]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(clientName))
            {
                continue;
            }

            var row = new Dictionary<string, object?>(orderedHeaders.Count);

            foreach (var header in orderedHeaders)
            {
                row[header] = tableColumns.Contains(header) ? dr[header] : null;
            }

            if (!statsByClient.TryGetValue(clientName, out var records))
            {
                records = new List<Dictionary<string, object?>>();
                statsByClient[clientName] = records;
            }

            records.Add(row);
        }
    }

    private static void ProcessFlatSheet(System.Data.DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
    {
        var tableColumns = new List<string>();

        foreach (System.Data.DataColumn col in table.Columns)
        {
            var colName = col.ColumnName ?? string.Empty;
            tableColumns.Add(colName);
            headers.Add(colName);
        }

        var orderedHeaders = headers.ToList();

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var row = new Dictionary<string, object?>(orderedHeaders.Count);

            for (int i = 0; i < tableColumns.Count; i++)
            {
                row[tableColumns[i]] = dr[i];
            }

            foreach (var header in orderedHeaders.Where(h => !row.ContainsKey(h)))
            {
                row[header] = null;
            }

            allRecords.Add(row);
        }
    }

    private static void WriteSummaryJson(string tmpDir, string fileName, Dictionary<string, double> metrics, (string, string)? headers)
    {
        if (metrics.Count == 0 || headers == null)
        {
            return;
        }

        var rows = metrics.Select(kv => new Dictionary<string, object?>
        {
            [headers.Value.Item1] = kv.Key,
            [headers.Value.Item2] = kv.Value
        }).ToList();

        WriteJson(tmpDir, fileName, rows);
    }

    private static void WriteKeyedJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        if (data.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    private static void WriteSimpleJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        if (data.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    private static void WriteClientStatsJson(string tmpDir, string fileName, Dictionary<string, List<Dictionary<string, object?>>> statsByClient)
    {
        if (statsByClient.Count == 0)
        {
            return;
        }

        var grouped = statsByClient.Select(kvp => new Dictionary<string, object?>
        {
            ["Client Name"] = kvp.Key,
            ["Records"] = DeduplicateRecords(kvp.Value)
        }).ToList();

        WriteJson(tmpDir, fileName, grouped);
    }

    private static void WriteFlatJson(string tmpDir, string fileName, List<Dictionary<string, object?>> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, DeduplicateRecords(records));
    }

    private static void WriteJson(string tmpDir, string fileName, object data)
    {
        var path = Path.Combine(tmpDir, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static HashSet<string> UpdateHeaders(System.Data.DataTable table, ICollection<string> headers)
    {
        var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Data.DataColumn col in table.Columns)
        {
            var colName = col.ColumnName ?? string.Empty;

            tableColumns.Add(colName);

            if (headers is HashSet<string> hs)
            {
                hs.Add(colName);
            }
            else if (headers is List<string> list && !list.Contains(colName))
            {
                list.Add(colName);
            }
        }

        return tableColumns;
    }

    private static Dictionary<string, object?> BuildRow(System.Data.DataTable table, List<string> headers)
    {
        return new Dictionary<string, object?>(headers.Count);
    }

    private static void MergeRows(Dictionary<string, object?> existing, Dictionary<string, object?> newRow, string keyColumn)
    {
        foreach (var kvp in newRow)
        {
            if (kvp.Key.Equals(keyColumn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var existingVal = existing.TryGetValue(kvp.Key, out var exVal) ? exVal : null;
            var newVal = kvp.Value;

            if (TryParseDouble(existingVal, out var existingNum) && TryParseDouble(newVal, out var newNum))
            {
                existing[kvp.Key] = existingNum + newNum;
            }
            else if (newVal != null && existingVal == null)
            {
                existing[kvp.Key] = newVal;
            }
        }
    }

    private static double ParseDoubleValue(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        var str = value.ToString();

        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        str = str?.Trim('"');

        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0;
    }

    private static bool TryParseDouble(object? val, out double result)
    {
        result = 0.0;

        if (val == null)
        {
            return false;
        }

        var str = val.ToString();

        if (string.IsNullOrEmpty(str))
        {
            return false;
        }

        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result) || double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    private static List<Dictionary<string, object?>> DeduplicateRecords(List<Dictionary<string, object?>> records)
    {
        var unique = new HashSet<string>();
        var result = new List<Dictionary<string, object?>>();

        foreach (var record in records)
        {
            var json = JsonSerializer.Serialize(record);

            if (unique.Add(json))
            {
                result.Add(record);
            }
        }

        return result;
    }
}