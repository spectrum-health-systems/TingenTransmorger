// 260206_code
// 260206_documentation

using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TingenTransmorger.TeleHealthReport;

internal static class ReportUtility
{
    /// <summary>Writes summary data as JSON with metric-value pairs.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="metrics">Dictionary of aggregated metrics.</param>
    /// <param name="headers">Optional column header names.</param>
    internal static void WriteSummaryJson(string tmpDir, string fileName, Dictionary<string, double> metrics, (string, string)? headers)
    {
        List<Dictionary<string, object?>> rows = [.. metrics.Select(kv => new Dictionary<string, object?>
        {
            [headers.Value.Item1] = kv.Key,
            [headers.Value.Item2] = kv.Value
        })];

        WriteJson(tmpDir, fileName, rows);
    }

    /// <summary>Writes keyed data as JSON array.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="data">Dictionary of records keyed by unique identifier.</param>
    internal static void WriteKeyedJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    /// <summary>Writes client statistics as JSON with nested records structure.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="statsByClient">Dictionary of record lists keyed by client name.</param>
    internal static void WriteClientStatsJson(string tmpDir, string fileName, Dictionary<string, List<Dictionary<string, object?>>> statsByClient)
    {
        var grouped = statsByClient.Select(kvp => new Dictionary<string, object?>
        {
            ["Client Name"] = kvp.Key,
            ["Records"] = DeduplicateRecords(kvp.Value.Select(record => 
                record.Where(field => !field.Key.Equals("Client Name", StringComparison.OrdinalIgnoreCase))
                      .ToDictionary(field => field.Key, field => field.Value)
            ).ToList())
        }).ToList();

        WriteJson(tmpDir, fileName, grouped);
    }

    /// <summary>Writes flat record list as JSON after de-duplication.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="records">List of records to write.</param>
    internal static void WriteFlatJson(string tmpDir, string fileName, List<Dictionary<string, object?>> records)
    {
        WriteJson(tmpDir, fileName, DeduplicateRecords(records));
    }

    /// <summary>Writes data to a JSON file with formatted indentation.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="fileName">Name of the JSON file to create.</param>
    /// <param name="data">Data object to serialize.</param>
    internal static void WriteJson(string tmpDir, string fileName, object data)
    {
        JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        var path = Path.Combine(tmpDir, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>Updates the headers collection with columns from the current table.</summary>
    /// <param name="table">DataTable to extract column names from.</param>
    /// <param name="headers">Collection to update with column names.</param>
    /// <returns>HashSet containing the current table's column names.</returns>
    internal static HashSet<string> UpdateHeaders(DataTable table, ICollection<string> headers)
    {
        var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (DataColumn col in table.Columns)
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
    internal static Dictionary<string, object?> BuildRow(DataTable table, List<string> headers)
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
            var newVal      = kvp.Value;

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

        return string.IsNullOrEmpty(str)
            ? false
            : double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result) || double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    /// <summary>Removes exact duplicate records from a list by comparing JSON serialization.</summary>
    /// <param name="records">List of records to de-duplicate.</param>
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