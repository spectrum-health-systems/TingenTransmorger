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

        // Normalize phone numbers before serialization
        var normalizedData = NormalizePhoneNumbersInData(data);

        var path = Path.Combine(tmpDir, fileName);
        var json = JsonSerializer.Serialize(normalizedData, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>
    /// Recursively normalizes phone number fields in data structures before JSON serialization.
    /// </summary>
    /// <param name="data">
    /// Data object that may contain phone number fields.
    /// </param>
    /// <returns>
    /// The data with phone number fields normalized.
    /// </returns>
    private static object NormalizePhoneNumbersInData(object data)
    {
        if (data is List<Dictionary<string, object?>> list)
        {
            return list.Select(dict => NormalizePhoneNumbersInDictionary(dict)).ToList();
        }
        else if (data is Dictionary<string, object?> dict)
        {
            return NormalizePhoneNumbersInDictionary(dict);
        }

        return data;
    }

    /// <summary>
    /// Normalizes phone number fields within a dictionary.
    /// </summary>
    /// <param name="dict">
    /// Dictionary that may contain phone number fields.
    /// </param>
    /// <returns>
    /// A new dictionary with phone number fields normalized.
    /// </returns>
    private static Dictionary<string, object?> NormalizePhoneNumbersInDictionary(Dictionary<string, object?> dict)
    {
        var normalized = new Dictionary<string, object?>();

        foreach (var kvp in dict)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            // Handle nested structures (like "Records" arrays in SMS Stats)
            if (value is List<Dictionary<string, object?>> nestedList)
            {
                normalized[key] = nestedList.Select(d => NormalizePhoneNumbersInDictionary(d)).ToList();
            }
            // Skip "Error Message" field - preserve full error text including opt-out messages
            else if (key.Equals("Error Message", StringComparison.OrdinalIgnoreCase))
            {
                normalized[key] = value;
            }
            // Check if this field is likely a phone number field
            else if (IsPhoneNumberField(key) && value != null)
            {
                var valueStr = value.ToString();
                normalized[key] = NormalizePhoneNumber(valueStr);
            }
            // Also normalize any string value that looks like a phone number
            else if (value is string strValue && !string.IsNullOrWhiteSpace(strValue) && LooksLikePhoneNumber(strValue))
            {
                normalized[key] = NormalizePhoneNumber(strValue);
            }
            else
            {
                normalized[key] = value;
            }
        }

        return normalized;
    }

    /// <summary>
    /// Determines if a field name likely contains phone number data.
    /// </summary>
    /// <param name="fieldName">
    /// Name of the field to check.
    /// </param>
    /// <returns>
    /// True if the field name suggests it contains phone number data.
    /// </returns>
    private static bool IsPhoneNumberField(string fieldName)
    {
        var lowerName = fieldName.ToLower();
        return lowerName.Contains("phone") || 
               lowerName.Contains("mobile") || 
               lowerName.Contains("cell") ||
               lowerName.Contains("telephone") ||
               lowerName.Contains("tel") ||
               lowerName.Contains("number") ||
               lowerName.Contains("to ") ||
               lowerName.Contains("from ") ||
               lowerName.Contains("recipient");
    }

    /// <summary>
    /// Determines if a string value looks like a phone number.
    /// </summary>
    /// <param name="value">
    /// String value to check.
    /// </param>
    /// <returns>
    /// True if the value appears to be a phone number.
    /// </returns>
    private static bool LooksLikePhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10)
        {
            return false;
        }

        // Count digits in the string
        var digitCount = value.Count(char.IsDigit);

        // If it has 10-11 digits and contains common phone number formatting characters, it's likely a phone number
        if (digitCount >= 10 && digitCount <= 11)
        {
            // Check for common phone number patterns
            return value.Any(c => c == '-' || c == '(' || c == ')' || c == '+' || c == ' ' || c == '.');
        }

        return false;
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

    /// <summary>
    /// Normalizes a phone number to exactly 10 digits by removing non-numeric characters and the leading '1' if present.
    /// </summary>
    /// <param name="phoneNumber">
    /// Raw phone number string that may contain formatting characters like '+', '-', '(', ')', spaces, etc.
    /// </param>
    /// <returns>
    /// A 10-digit phone number string (area code + local number) if the input can be normalized; 
    /// otherwise, returns the original value if it cannot be normalized to exactly 10 digits.
    /// </returns>
    /// <remarks>
    /// This method ensures phone numbers are stored in JSON files with consistent formatting:
    /// - All non-numeric characters ('+', '-', '(', ')', spaces, etc.) are removed
    /// - Leading '1' is removed if the number is 11 digits (common in North American phone numbers)
    /// - Only returns the normalized value if it results in exactly 10 digits
    /// </remarks>
    internal static string NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return phoneNumber ?? string.Empty;
        }

        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Remove leading '1' if the number is 11 digits
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits.Substring(1);
        }

        // Only return normalized value if it's exactly 10 digits, otherwise return original
        return digits.Length == 10 ? digits : phoneNumber;
    }
}

