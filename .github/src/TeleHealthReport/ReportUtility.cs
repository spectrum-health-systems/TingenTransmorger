// 260206_code
// 260206_documentation

using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>
/// Utility helpers used by the TeleHealth report processors for building, merging and emitting report data. All members
/// are internal because these functions are intended to be consumed by other types within the TeleHealthReport
/// namespace only.
/// </summary>
/// <remarks>
/// - Serialization uses <see cref="System.Text.Json.JsonSerializer"/> with indentation enabled.
/// - Numeric parsing tries invariant culture first, then falls back to the current culture to accommodate
///   locale-specific number formats encountered in input files.
/// - Many helpers operate on dictionaries of <see cref="object"/> values; callers are responsible for providing
///   consistent keys (header names) when aggregating or merging rows.
/// - Methods intentionally avoid throwing for typical parsing and missing-column situations; instead they return
///   defaults (e.g. 0 for numeric parsing) or no-ops when required inputs are absent. This makes the utility tolerant
///   of variable input spreadsheets.
/// </remarks>
internal static class ReportUtility
{
    /// <summary>
    /// Writes summary data as a JSON array of metric/value objects.
    /// </summary>
    /// <param name="tmpDir">
    /// Output directory where the file will be written. The directory is not created by this method.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to create (relative file name).
    /// </param>
    /// <param name="metrics">
    /// Dictionary of aggregated metrics where the key is the metric name and the value is the numeric metric.
    /// </param>
    /// <param name="headers">
    /// Optional tuple containing the header names to be used for the metric name and metric value in the produced JSON
    /// objects. When <c>null</c>, callers should ensure headers are set prior to calling this method (the code
    /// assumes <see cref="headers.Value"/> when constructing rows).
    /// </param>
    internal static void WriteSummaryJson(string tmpDir, string fileName, Dictionary<string, double> metrics, (string, string)? headers)
    {
        List<Dictionary<string, object?>> rows = [.. metrics.Select(kv => new Dictionary<string, object?>
        {
            [headers.Value.Item1] = kv.Key,
            [headers.Value.Item2] = kv.Value
        })];

        WriteJson(tmpDir, fileName, rows);
    }

    /// <summary>
    /// Writes a dictionary of keyed records as a JSON array containing the values.
    /// </summary>
    /// <param name="tmpDir">
    /// Output directory where the file will be written.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to create.
    /// </param>
    /// <param name="data">
    /// Dictionary of records keyed by a unique identifier; only the record values are serialized.
    /// </param>
    internal static void WriteKeyedJson(string tmpDir, string fileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        WriteJson(tmpDir, fileName, data.Values.ToList());
    }

    /// <summary>
    /// Writes client statistics as JSON. Each client becomes an object with a "Client Name" property and a "Records"
    /// property that contains an array of de-duplicated records for that client.
    /// </summary>
    /// <param name="tmpDir">
    /// Output directory where the file will be written.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to create.
    /// </param>
    /// <param name="statsByClient">
    /// Dictionary mapping client names to lists of record dictionaries.
    /// </param>
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

    /// <summary>
    /// Writes a flat list of records to JSON after removing exact duplicates.
    /// </summary>
    /// <param name="tmpDir">
    /// Output directory where the file will be written.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to create.
    /// </param>
    /// <param name="records">
    /// List of record dictionaries to serialize; duplicates (by JSON content) are removed prior to writing.
    /// </param>
    internal static void WriteFlatJson(string tmpDir, string fileName, List<Dictionary<string, object?>> records)
    {
        WriteJson(tmpDir, fileName, DeduplicateRecords(records));
    }

    /// <summary>
    /// Serializes the provided data object to a JSON file using indented formatting.
    /// </summary>
    /// <param name="tmpDir">
    /// Output directory where the file will be written.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to create.
    /// </param>
    /// <param name="data">
    /// The data object to serialize. Can be a list, dictionary, or any serializable object.
    /// </param>
    internal static void WriteJson(string tmpDir, string fileName, object data)
    {
        JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        var path = Path.Combine(tmpDir, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>
    /// Updates the provided headers collection with column names from the supplied <see cref="DataTable"/>.
    /// </summary>
    /// <param name="table">
    /// DataTable to extract column names from.
    /// </param>
    /// <param name="headers">
    /// Collection to update with column names. The method supports both <see cref="HashSet{T}"/> and
    /// <see cref="List{T}"/> implementations; other implementations will not be modified but the method
    /// still returns the table's columns as a <see cref="HashSet{T}"/>.
    /// </param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> (case-insensitive) containing the column names present in <paramref name="table"/>.
    /// </returns>
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
    /// <summary>
    /// Creates an empty dictionary sized for the expected number of columns.
    /// </summary>
    /// <param name="table">
    /// DataTable (currently unused; present for potential future optimizations).
    /// </param>
    /// <param name="headers">
    /// List of column headers used to initialize the dictionary capacity.
    /// </param>
    /// <returns>
    /// An empty <see cref="Dictionary{TKey, TValue}"/> pre-allocated with capacity equal to <paramref name="headers"/>.Count.
    /// </returns>
    internal static Dictionary<string, object?> BuildRow(DataTable table, List<string> headers)
    {
        return new Dictionary<string, object?>(headers.Count);
    }

    /// <summary>
    /// Merges values from <paramref name="newRow"/> into <paramref name="existing"/>.
    /// </summary>
    /// <remarks>
    /// - Skips the column named by <paramref name="keyColumn"/>.
    /// - If both values are numeric (parseable as double) they are summed.
    /// - If the existing value is <c>null</c> and the new value is non-null, the new value replaces it.
    /// </remarks>
    /// <param name="existing">
    /// Existing row dictionary to update.
    /// </param>
    /// <param name="newRow">
    /// New row dictionary with values to merge.
    /// </param>
    /// <param name="keyColumn">
    /// Key column name to skip during merge (case-insensitive comparison).
    /// </param>
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

    /// <summary>
    /// Parses an object to a <see cref="double"/>, tolerating common input formats.
    /// </summary>
    /// <param name="value">
    /// Value to parse; may be a numeric type, a numeric string, or a quoted numeric string.
    /// </param>
    /// <returns>
    /// The parsed double, or <c>0</c> if parsing fails or <paramref name="value"/> is <c>null</c>.
    /// </returns>
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

    /// <summary>
    /// Attempts to parse an object as a double using invariant culture first, then the current culture.
    /// </summary>
    /// <param name="val">Value to parse.
    /// </param>
    /// <param name="result">
    /// Parsed double value if successful; otherwise 0.
    /// </param>
    /// <returns>
    /// <c>true</c> if parsing succeeded; otherwise <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Removes exact duplicate records from a list by comparing each record's JSON serialization.
    /// </summary>
    /// <param name="records">
    /// List of records to de-duplicate. The original record objects are preserved in the returned list.
    /// </param>
    /// <returns>
    /// A new list containing only the first occurrence of each unique record (based on serialized JSON).
    /// </returns>
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