// 260205_code
// 260205_documentation

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ExcelDataReader;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Processes TeleHealth Excel reports and converts them to JSON format.</summary>
/// <remarks>
///     This processor handles four types of reports:
///     <list type="bullet">
///         <item>Visit Stats - Summary and Meeting Errors</item>
///         <item>Visit Details - Meeting Details and Participant Details</item>
///         <item>Message Failure - Summary, SMS Stats, and Email Stats</item>
///         <item>Message Delivery - Message Delivery Stats</item>
///     </list>
/// </remarks>
class ReportProcessor
{
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Processes all TeleHealth reports from the import directory and generates JSON output files.</summary>
    /// <param name="config">Configuration object containing import and output directory paths.</param>
    internal static void Process(string importDir, string tmpDir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);

        VisitStatsReport.ProcessVisitStatsReports(importDir, tmpDir);
        VisitDetailReport.ProcessVisitDetailsReports(importDir, tmpDir);
        MessageFailureReport.ProcessMessageFailureReports(importDir, tmpDir);
        MessageDeliveryReport.ProcessMessageDeliveryReports(importDir, tmpDir);
    }

    /// <summary>Processes Excel files matching a pattern and invokes a callback for each worksheet.</summary>
    /// <param name="importDir">Directory to search for Excel files.</param>
    /// <param name="pattern">File search pattern (e.g., "*Visit_Stats*.xlsx").</param>
    /// <param name="processSheet">Callback action that receives each DataTable and sheet name.</param>
    internal static void ProcessExcelFiles(string importDirectory, string pattern, Action<System.Data.DataTable, string> processSheet)
    {
        var matchingFiles = Directory.GetFiles(importDirectory, pattern, SearchOption.TopDirectoryOnly);

        foreach (var filePath in matchingFiles)
        {
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var excelReader = ExcelReaderFactory.CreateReader(fileStream);

            var dataSet = excelReader.AsDataSet(ExcelConfig); //

            foreach (System.Data.DataTable worksheet in dataSet.Tables)
            {
                if (worksheet.Columns.Count > 0)
                {
                    processSheet(worksheet, worksheet.TableName);
                }
            }
        }
    }

    /////// <summary>Processes summary sheets with key-value pairs, aggregating numeric values across files.</summary>
    /////// <param name="table">DataTable containing the summary sheet data.</param>
    /////// <param name="metrics">Dictionary to store aggregated metrics.</param>
    /////// <param name="headers">Optional tuple to capture column header names.</param>
    ////internal static void ProcessSummarySheet(System.Data.DataTable table, Dictionary<string, double> metrics, ref (string, string)? headers)
    ////{
    ////    if (table.Columns.Count < 2)
    ////    {
    ////        return;
    ////    }

    ////    headers ??= (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");

    ////    foreach (System.Data.DataRow dataRow in table.Rows)
    ////    {
    ////        var metricKey = dataRow[0]?.ToString()?.Trim();

    ////        if (string.IsNullOrEmpty(metricKey))
    ////        {
    ////            continue;
    ////        }

    ////        var metricValue = ParseDoubleValue(dataRow[1]);

    ////        if (metrics.TryGetValue(metricKey, out var existingValue))
    ////        {
    ////            metrics[metricKey] = existingValue + metricValue;
    ////        }
    ////        else
    ////        {
    ////            metrics[metricKey] = metricValue;
    ////        }
    ////    }
    ////}

    /////// <summary>Processes sheets with a unique key column, optionally aggregating numeric values for duplicate keys.</summary>
    /////// <param name="table">DataTable containing the sheet data.</param>
    /////// <param name="dataById">Dictionary to store records keyed by the specified column.</param>
    /////// <param name="headers">List to track all column headers encountered.</param>
    /////// <param name="keyColumn">Name of the column to use as the unique key.</param>
    /////// <param name="aggregateNumeric">If true, numeric values are summed for duplicate keys.</param>
    ////internal static void ProcessKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById,
    ////    List<string> headers, string keyColumn, bool aggregateNumeric = false)
    ////{
    ////    if (!table.Columns.Contains(keyColumn))
    ////        return;

    ////    UpdateHeaders(table, headers);

    ////    foreach (System.Data.DataRow dr in table.Rows)
    ////    {
    ////        var key = dr[keyColumn]?.ToString()?.Trim();

    ////        if (string.IsNullOrEmpty(key))
    ////        {
    ////            continue;
    ////        }

    ////        var row = BuildRow(table, headers);

    ////        foreach (var header in headers)
    ////        {
    ////            row[header] = table.Columns.Contains(header) ? dr[header] : null;
    ////        }

    ////        if (dataById.TryGetValue(key, out var existingRow) && aggregateNumeric)
    ////        {
    ////            MergeRows(existingRow, row, keyColumn);
    ////        }
    ////        else
    ////        {
    ////            dataById.TryAdd(key, row);
    ////        }
    ////    }
    ////}

    /////// <summary>Processes sheets with a unique key column, keeping only the first occurrence of each key.</summary>
    /////// <param name="table">DataTable containing the sheet data.</param>
    /////// <param name="dataById">Dictionary to store records keyed by the specified column.</param>
    /////// <param name="headers">HashSet to track all column headers encountered.</param>
    /////// <param name="keyColumn">Name of the column to use as the unique key.</param>
    ////internal static void ProcessSimpleKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, HashSet<string> headers, string keyColumn)
    ////{
    ////    if (!table.Columns.Contains(keyColumn))
    ////    {
    ////        return;
    ////    }

    ////    var tableColumns = UpdateHeaders(table, headers);
    ////    var orderedHeaders = headers.ToList();

    ////    foreach (System.Data.DataRow dr in table.Rows)
    ////    {
    ////        var key = dr[keyColumn]?.ToString()?.Trim();

    ////        if (string.IsNullOrEmpty(key))
    ////        {
    ////            continue;
    ////        }

    ////        if (!dataById.ContainsKey(key))
    ////        {
    ////            var row = new Dictionary<string, object?>(orderedHeaders.Count);

    ////            foreach (var header in orderedHeaders)
    ////            {
    ////                row[header] = tableColumns.Contains(header) ? dr[header] : null;
    ////            }

    ////            dataById[key] = row;
    ////        }
    ////    }
    ////}

    /////// <summary>Processes sheets with client statistics, allowing multiple records per client.</summary>
    /////// <param name="table">DataTable containing the sheet data.</param>
    /////// <param name="statsByClient">Dictionary to store lists of records per client.</param>
    /////// <param name="headers">HashSet to track all column headers encountered.</param>
    ////internal static void ProcessClientStatsSheet(System.Data.DataTable table, Dictionary<string, List<Dictionary<string, object?>>> statsByClient, HashSet<string> headers)
    ////{
    ////    if (!table.Columns.Contains("Client Name"))
    ////    {
    ////        return;
    ////    }

    ////    var tableColumns = UpdateHeaders(table, headers);
    ////    var orderedHeaders = headers.ToList();

    ////    foreach (System.Data.DataRow dr in table.Rows)
    ////    {
    ////        var clientName = dr["Client Name"]?.ToString()?.Trim();

    ////        if (string.IsNullOrEmpty(clientName))
    ////        {
    ////            continue;
    ////        }

    ////        var row = new Dictionary<string, object?>(orderedHeaders.Count);

    ////        foreach (var header in orderedHeaders)
    ////        {
    ////            row[header] = tableColumns.Contains(header) ? dr[header] : null;
    ////        }

    ////        if (!statsByClient.TryGetValue(clientName, out var records))
    ////        {
    ////            records = new List<Dictionary<string, object?>>();
    ////            statsByClient[clientName] = records;
    ////        }

    ////        records.Add(row);
    ////    }
    ////}

    /////// <summary>Processes sheets as flat record lists, capturing all rows without keying or aggregation.</summary>
    /////// <param name="table">DataTable containing the sheet data.</param>
    /////// <param name="allRecords">List to store all records.</param>
    /////// <param name="headers">HashSet to track all column headers encountered.</param>
    ////internal static void ProcessFlatSheet(System.Data.DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
    ////{
    ////    var tableColumns = new List<string>();

    ////    foreach (System.Data.DataColumn col in table.Columns)
    ////    {
    ////        var colName = col.ColumnName ?? string.Empty;
    ////        tableColumns.Add(colName);
    ////        headers.Add(colName);
    ////    }

    ////    var orderedHeaders = headers.ToList();

    ////    foreach (System.Data.DataRow dr in table.Rows)
    ////    {
    ////        var row = new Dictionary<string, object?>(orderedHeaders.Count);

    ////        for (int i = 0; i < tableColumns.Count; i++)
    ////        {
    ////            row[tableColumns[i]] = dr[i];
    ////        }

    ////        foreach (var header in orderedHeaders.Where(h => !row.ContainsKey(h)))
    ////        {
    ////            row[header] = null;
    ////        }

    ////        allRecords.Add(row);
    ////    }
    ////}

    /// <summary>Writes summary data as JSON with metric-value pairs.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="metrics">Dictionary of aggregated metrics.</param>
    /// <param name="headers">Optional column header names.</param>
    internal static void WriteSummaryJson(string tmpDir, string fileName, Dictionary<string, double> metrics, (string, string)? headers)
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

    /// <summary>Writes keyed data as JSON array.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="data">Dictionary of records keyed by unique identifier.</param>
    internal static void WriteKeyedJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        if (data.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    /// <summary>Writes simple keyed data as JSON array.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="data">Dictionary of records keyed by unique identifier.</param>
    internal static void WriteSimpleJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        if (data.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    /// <summary>Writes client statistics as JSON with nested records structure.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="statsByClient">Dictionary of record lists keyed by client name.</param>
    internal static void WriteClientStatsJson(string tmpDir, string fileName, Dictionary<string, List<Dictionary<string, object?>>> statsByClient)
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

    /// <summary>Writes flat record list as JSON after deduplication.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="records">List of records to write.</param>
    internal static void WriteFlatJson(string tmpDir, string fileName, List<Dictionary<string, object?>> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        WriteJson(tmpDir, fileName, DeduplicateRecords(records));
    }

    /// <summary>Writes data to a JSON file with formatted indentation.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="data">Data object to serialize.</param>
    internal static void WriteJson(string tmpDir, string fileName, object data)
    {
        var path = Path.Combine(tmpDir, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>Updates the headers collection with columns from the current table.</summary>
    /// <param name="table">DataTable to extract column names from.</param>
    /// <param name="headers">Collection to update with column names.</param>
    /// <returns>HashSet containing the current table's column names.</returns>
    internal static HashSet<string> UpdateHeaders(System.Data.DataTable table, ICollection<string> headers)
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

    /// <summary>Creates an empty dictionary sized for the expected number of columns.</summary>
    /// <param name="table">DataTable (currently unused, reserved for future optimization).</param>
    /// <param name="headers">List of column headers to determine dictionary capacity.</param>
    /// <returns>Empty dictionary with pre-allocated capacity.</returns>
    internal static Dictionary<string, object?> BuildRow(System.Data.DataTable table, List<string> headers)
    {
        return new Dictionary<string, object?>(headers.Count);
    }

    /// <summary>Merges a new row into an existing row, aggregating numeric values and preserving non-null data.</summary>
    /// <param name="existing">Existing row dictionary to update.</param>
    /// <param name="newRow">New row dictionary with values to merge.</param>
    /// <param name="keyColumn">Key column name to skip during merge.</param>
    internal static void MergeRows(Dictionary<string, object?> existing, Dictionary<string, object?> newRow, string keyColumn)
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

    /// <summary>Parses an object as a double value, handling various formats including quoted strings.</summary>
    /// <param name="value">Value to parse.</param>
    /// <returns>Parsed double value, or 0 if parsing fails.</returns>
    internal static double ParseDoubleValue(object? value)
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

    /// <summary>Attempts to parse an object as a double value using invariant and current culture.</summary>
    /// <param name="val">Value to parse.</param>
    /// <param name="result">Parsed double value if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    internal static bool TryParseDouble(object? val, out double result)
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

    /// <summary>Removes exact duplicate records from a list by comparing JSON serialization.</summary>
    /// <param name="records">List of records to deduplicate.</param>
    /// <returns>List containing only unique records.</returns>
    internal static List<Dictionary<string, object?>> DeduplicateRecords(List<Dictionary<string, object?>> records)
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