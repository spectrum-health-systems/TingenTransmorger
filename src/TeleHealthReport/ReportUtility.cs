// 260310_code
// 260310_documentation

using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Provides utility methods for building, normalizing, and writing JSON report output files.</summary>
/// <remarks>
/// This class centralizes shared logic used across TeleHealth report generation, including:
/// <list type="bullet">
/// <item>Writing flat, keyed, summary, and client-grouped JSON files.</item>
/// <item>De-duplicating records before serialization.</item>
/// <item>Normalizing phone number values in record dictionaries.</item>
/// <item>Building and merging row dictionaries sourced from <see cref="DataTable"/> instances.</item>
/// <item>Parsing object values as <see cref="double"/> with culture and quote-trimming fallbacks.</item>
/// </list>
/// All members are <see langword="internal"/> and <see langword="static"/>; this class is not intended for
/// instantiation or external use.
/// </remarks>
internal static class ReportUtility
{
    /// <summary>Writes summary details.</summary>
    /// <remarks>
    /// This is specifically designed for the following workbook/worksheets:
    /// <list type="bullet">
    /// <item>Visit_Stats/Summary</item>
    /// <item>Message_Failure/Summary</item>
    /// </list>
    /// These worksheets consist of key-value pairs, where keys are metric names and values are their corresponding
    /// double values. The headers parameter allows for custom column names in the output JSON, but if not provided, it
    /// will default to "Metric" and "Value". The method transforms the dictionary into a list of records suitable for
    /// JSON serialization, ensuring that the output is structured as an array of objects with consistent formatting.
    /// </remarks>
    /// <param name="targetDir">Target output directory.</param>
    /// <param name="targetFileName">Target output file name.</param>
    /// <param name="metrics">Dictionary of metric names and their corresponding double values.</param>
    /// <param name="headers">Tuple containing the column header names for the metric key and value columns.</param>
    internal static void WriteSummaryJson(string targetDir, string targetFileName, Dictionary<string, double> metrics, (string, string)? headers)
    {
        List<Dictionary<string, object?>> rows = [.. metrics.Select(keyValuePair => new Dictionary<string, object?>
        {
            [headers.Value.Item1] = keyValuePair.Key,
            [headers.Value.Item2] = keyValuePair.Value
        })];

        WriteJson(targetDir, targetFileName, rows);
    }

    /// <summary>Writes dictionary-keyed data as a flat JSON array.</summary>
    /// <remarks>
    /// Extracts the values from the provided dictionary and writes them as a JSON array, discarding the outer string
    /// keys. This is useful when the dictionary keys are used only for internal lookup and the output should be a flat
    /// list of records.
    /// </remarks>
    /// <param name="targetDir">Target output directory.</param>
    /// <param name="targetFileName">Target output file name.</param>
    /// <param name="data">Dictionary of keyed records whose values will be written as a JSON array.</param>
    internal static void WriteKeyedJson(string targetDir, string targetFileName, Dictionary<string, Dictionary<string, object?>> data)
    {
        WriteJson(targetDir, targetFileName, data.Values.ToList());
    }

    /// <summary>Groups client statistics by client name and writes the result as a nested JSON file.</summary>
    /// <remarks>
    /// Transforms a flat dictionary of client-keyed record lists into a structured JSON output where each entry
    /// contains a <c>Client Name</c> field and a nested <c>Records</c> array. The <c>Client Name</c> field is removed
    /// from each individual record to avoid redundancy, since it is already present at the top level. Duplicate records
    /// within each client's record list are removed before serialization via <see cref="DeduplicateRecords"/>.
    /// </remarks>
    /// <param name="targetDir">Target output directory.</param>
    /// <param name="targetFileName">Target output file name.</param>
    /// <param name="statsByClient">Dictionary mapping each client name to a list of records for that client.</param>
    internal static void WriteClientStatsJson(string targetDir, string targetFileName, Dictionary<string, List<Dictionary<string, object?>>> statsByClient)
    {
        var grouped = statsByClient.Select(keyValuePair => new Dictionary<string, object?>
        {
            ["Client Name"] = keyValuePair.Key,
            ["Records"] = DeduplicateRecords(keyValuePair.Value.Select(record =>
                record.Where(field => !field.Key.Equals("Client Name", StringComparison.OrdinalIgnoreCase))
                      .ToDictionary(field => field.Key, field => field.Value)
            ).ToList())
        }).ToList();

        WriteJson(targetDir, targetFileName, grouped);
    }

    /// <summary>Writes a de-duplicated list of records as a flat JSON array.</summary>
    /// <remarks>
    /// Removes exact duplicate records before writing by delegating to <see cref="DeduplicateRecords"/>, then
    /// serializes the result as a flat JSON array via <see cref="WriteJson"/>. Use this method when the input may
    /// contain duplicate entries that should not appear in the output file.
    /// </remarks>
    /// <param name="targetDir">Target output directory.</param>
    /// <param name="targetFileName">Target output file name.</param>
    /// <param name="records">List of records to de-duplicate and write as a JSON array.</param>
    internal static void WriteFlatJson(string targetDir, string targetFileName, List<Dictionary<string, object?>> records)
    {
        WriteJson(targetDir, targetFileName, DeduplicateRecords(records));
    }

    /// <summary>Serializes data to a formatted JSON file at the specified path.</summary>
    /// <remarks>
    /// Phone number fields within the data are normalized before serialization via
    /// <see cref="NormalizePhoneNumbersInData"/>, ensuring consistent formatting in the output file. The JSON output is
    /// written with indentation enabled and encoded as UTF-8.
    /// </remarks>
    /// <param name="targetDir">Target output directory.</param>
    /// <param name="targetFileName">Target output file name.</param>
    /// <param name="data">Data object to serialize; may be a flat list, dictionary, or nested structure.</param>
    internal static void WriteJson(string targetDir, string targetFileName, object data)
    {
        JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        var normalizedData = NormalizePhoneNumbersInData(data);

        var path = Path.Combine(targetDir, targetFileName);
        var json = JsonSerializer.Serialize(normalizedData, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>
    /// Normalizes phone numbers in the provided data object.
    /// </summary>
    /// <remarks>
    /// Handles two data shapes:
    /// <list type="bullet">
    /// <item>
    /// A <see cref="List{T}"/> of <see cref="Dictionary{TKey, TValue}"/> records — each dictionary is
    /// normalized individually via <see cref="NormalizePhoneNumbersInDictionary"/>.
    /// </item>
    /// <item>
    /// A single <see cref="Dictionary{TKey, TValue}"/> — normalized directly via
    /// <see cref="NormalizePhoneNumbersInDictionary"/>.
    /// </item>
    /// </list>
    /// If the data does not match either of these shapes, it is returned unchanged.
    /// </remarks>
    /// <param name="data">Data object to normalize; may be a flat list of records or a single record dictionary.</param>
    /// <returns>
    /// The normalized data object with phone number fields formatted consistently. If the input type is not recognized,
    /// the original object is returned unmodified.
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
    /// Normalizes phone number fields within a single record dictionary.
    /// </summary>
    /// <remarks>
    /// Iterates over each key-value pair in the dictionary and applies normalization according to the following rules:
    /// <list type="bullet">
    /// <item>
    /// If the value is a nested <see cref="List{T}"/> of <see cref="Dictionary{TKey, TValue}"/> records (e.g., a
    /// <c>Records</c> array in SMS Stats), each nested dictionary is recursively normalized via this method.
    /// </item>
    /// <item>
    /// If the key matches <c>Error Message</c> (case-insensitive), the value is preserved as-is to retain full error
    /// text, including opt-out messages that may contain phone-number-like strings.
    /// </item>
    /// <item>
    /// If the key is identified as a phone number field by <see cref="IsPhoneNumberField"/>, the value is normalized
    /// via <see cref="NormalizePhoneNumber"/>.
    /// </item>
    /// <item>
    /// If the value is a non-empty string that resembles a phone number as determined by
    /// <see cref="LooksLikePhoneNumber"/>, it is also normalized via <see cref="NormalizePhoneNumber"/>.
    /// </item>
    /// <item>All other values are copied to the result dictionary unchanged.</item>
    /// </list>
    /// </remarks>
    /// <param name="dict">Record dictionary whose phone number fields will be normalized.</param>
    /// <returns>
    /// A new dictionary with the same keys as the input, where phone number values have been normalized to a
    /// consistent 10-digit format and all other values are preserved as-is.
    /// </returns>
    private static Dictionary<string, object?> NormalizePhoneNumbersInDictionary(Dictionary<string, object?> dict)
    {
        var normalized = new Dictionary<string, object?>();

        foreach (var keyValuePair in dict)
        {
            var key   = keyValuePair.Key;
            var value = keyValuePair.Value;

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
            else
            {
                normalized[key] = value is string strValue && !string.IsNullOrWhiteSpace(strValue) && LooksLikePhoneNumber(strValue)
                    ? NormalizePhoneNumber(strValue)
                    : value;
            }
        }

        return normalized;
    }

    /// <summary>Determines whether a field name is likely to contain a phone number.</summary>
    /// <remarks>
    /// Performs a case-insensitive substring match against common phone number field name patterns, including:
    /// <list type="bullet">
    /// <item><c>phone</c></item>
    /// <item><c>mobile</c></item>
    /// <item><c>cell</c></item>
    /// <item><c>telephone</c></item>
    /// <item><c>tel</c></item>
    /// <item><c>number</c></item>
    /// <item><c>to </c></item>
    /// <item><c>from </c></item>
    /// <item><c>recipient</c></item>
    /// </list>
    /// This method is used in conjunction with <see cref="LooksLikePhoneNumber"/> to identify values that should be
    /// normalized via <see cref="NormalizePhoneNumber"/>.
    /// </remarks>
    /// <param name="fieldName">Field name to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the field name contains a recognized phone number keyword; otherwise,
    /// <see langword="false"/>.
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

    /// <summary>Determines whether a string value resembles a formatted phone number.</summary>
    /// <remarks>
    /// Applies the following heuristics to evaluate the value:
    /// <list type="bullet">
    /// <item>The value must be at least 10 characters long and non-whitespace.</item>
    /// <item>The value must contain between 10 and 11 digit characters.</item>
    /// <item>
    /// The value must contain at least one common phone number formatting character:
    /// <c>-</c>, <c>(</c>, <c>)</c>, <c>+</c>, a space, or <c>.</c>
    /// </item>
    /// </list>
    /// This method is used in conjunction with <see cref="IsPhoneNumberField"/> to identify values that should be
    /// normalized via <see cref="NormalizePhoneNumber"/>. Unlike <see cref="IsPhoneNumberField"/>, which evaluates the
    /// field name, this method evaluates the field value itself.
    /// </remarks>
    /// <param name="value">String value to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the value contains 10–11 digits and at least one recognized phone number formatting
    /// character; otherwise, <see langword="false"/>.
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

    /// <summary>Extracts column names from a <see cref="DataTable"/> and adds them to the header collection.</summary>
    /// <remarks>
    /// Iterates over each column in the provided <paramref name="table"/> and performs two operations:
    /// <list type="bullet">
    /// <item>
    /// Adds the column name to the returned <see cref="HashSet{T}"/>, which uses
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> for case-insensitive membership checks.
    /// </item>
    /// <item>
    /// Adds the column name to the shared <paramref name="headers"/> collection, with behavior that depends on the
    /// runtime type of the collection:
    /// <list type="bullet">
    /// <item>
    /// If <paramref name="headers"/> is a <see cref="HashSet{T}"/>, the column name is added directly (the set
    /// handles deduplication internally).
    /// </item>
    /// <item>
    /// If <paramref name="headers"/> is a <see cref="List{T}"/>, the column name is only added if it is not already
    /// present, preventing duplicates.
    /// </item>
    /// </list>
    /// </item>
    /// </list>
    /// A <see langword="null"/> column name is treated as an empty string.
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> whose columns will be enumerated.</param>
    /// <param name="headers">
    /// Shared header collection to populate; supports both <see cref="HashSet{T}"/> and <see cref="List{T}"/>
    /// implementations of <see cref="ICollection{T}"/>.
    /// </param>
    /// <returns>
    /// A case-insensitive <see cref="HashSet{T}"/> containing all column names found in <paramref name="table"/>.
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

    /// <summary>Builds an empty row dictionary pre-allocated to the number of headers.</summary>
    /// <remarks>
    /// Creates a new <see cref="Dictionary{TKey, TValue}"/> with an initial capacity equal to the number of entries in
    /// <paramref name="headers"/>, avoiding internal resizing when fields are subsequently populated. The returned
    /// dictionary contains no entries and is intended to be filled by the caller.
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> associated with the row being built.</param>
    /// <param name="headers">List of column header names used to determine the initial capacity of the dictionary.</param>
    /// <returns>
    /// An empty <see cref="Dictionary{TKey, TValue}"/> with a capacity equal to <c>headers.Count</c>, ready to be
    /// populated with field values.
    /// </returns>
    internal static Dictionary<string, object?> BuildRow(DataTable table, List<string> headers)
    {
        return new Dictionary<string, object?>(headers.Count);
    }

    /// <summary>Merges fields from a new row into an existing row, accumulating numeric values.</summary>
    /// <remarks>
    /// Iterates over each field in <paramref name="newRow"/> and merges it into <paramref name="existing"/> according
    /// to the following rules:
    /// <list type="bullet">
    /// <item>
    /// The field identified by <paramref name="keyColumn"/> is skipped, as it serves as the merge key and should not
    /// be overwritten or accumulated.
    /// </item>
    /// <item>
    /// If both the existing value and the new value can be parsed as doubles via <see cref="TryParseDouble"/>, their
    /// values are summed and stored back into <paramref name="existing"/>.
    /// </item>
    /// <item>
    /// If the new value is non-<see langword="null"/> and no existing value is present for the field, the new value is
    /// copied into <paramref name="existing"/> as-is.
    /// </item>
    /// <item>All other field combinations are left unchanged in <paramref name="existing"/>.</item>
    /// </list>
    /// This method modifies <paramref name="existing"/> in place and does not return a value.
    /// </remarks>
    /// <param name="existing">The target row dictionary to merge values into; modified in place.</param>
    /// <param name="newRow">The source row dictionary whose fields will be merged into <paramref name="existing"/>.</param>
    /// <param name="keyColumn">
    /// The name of the key column to skip during merging; comparison is case-insensitive.
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

    /// <summary>Parses an object value as a double, with fallback quote-trimming on failure.</summary>
    /// <remarks>
    /// Attempts to convert the provided value to a double using the following strategy:
    /// <list type="bullet">
    /// <item>
    /// If <paramref name="value"/> is <see langword="null"/>, returns <c>0</c> immediately.
    /// </item>
    /// <item>
    /// Converts the value to its string representation and attempts to parse it using
    /// <see cref="NumberStyles.Any"/> and <see cref="CultureInfo.InvariantCulture"/>.
    /// </item>
    /// <item>
    /// If the initial parse fails, surrounding quotation marks are trimmed from the string and parsing
    /// is attempted a second time under the same culture settings. This handles values that may have been
    /// serialized with enclosing quotes (e.g., <c>"3.14"</c>).
    /// </item>
    /// <item>If both attempts fail, returns <c>0</c>.</item>
    /// </list>
    /// To also determine whether parsing succeeded, use <see cref="TryParseDouble"/> instead.
    /// </remarks>
    /// <param name="value">Value to parse; may be a string, boxed numeric type, or <see langword="null"/>.</param>
    /// <returns>
    /// The parsed <see cref="double"/> value if successful; otherwise, <c>0</c>.
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

    /// <summary>Attempts to parse an object value as a double, trying invariant culture before reverting to current culture.</summary>
    /// <remarks>
    /// Attempts to convert the provided value to a double using the following strategy:
    /// <list type="bullet">
    /// <item>
    /// If <paramref name="val"/> is <see langword="null"/>, sets <paramref name="result"/> to <c>0</c> and returns
    /// <see langword="false"/> immediately.
    /// </item>
    /// <item>
    /// Converts the value to its string representation; if the result is <see langword="null"/> or empty, returns
    /// <see langword="false"/>.
    /// </item>
    /// <item>
    /// Attempts to parse the string using <see cref="NumberStyles.Any"/> and
    /// <see cref="CultureInfo.InvariantCulture"/>. If that fails, a second attempt is made using
    /// <see cref="CultureInfo.CurrentCulture"/> to accommodate locale-specific numeric formats.
    /// </item>
    /// </list>
    /// Unlike <see cref="ParseDoubleValue"/>, this method uses a standard <c>TryParse</c> pattern — it returns a
    /// boolean indicating success or failure rather than silently returning <c>0</c> on failure.
    /// </remarks>
    /// <param name="val">Value to parse; may be a string, boxed numeric type, or <see langword="null"/>.</param>
    /// <param name="result">
    /// When this method returns <see langword="true"/>, contains the parsed <see cref="double"/> value; otherwise,
    /// contains <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="val"/> was successfully parsed as a <see cref="double"/>;
    /// otherwise, <see langword="false"/>.
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

    /// <summary>Removes exact duplicate records from a list by comparing their serialized JSON representations.</summary>
    /// <remarks>
    /// Iterates over each record in <paramref name="records"/> and serializes it to a JSON string via
    /// <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)"/>. A <see cref="HashSet{T}"/> of
    /// these serialized strings is used to detect duplicates — only the first occurrence of each unique JSON
    /// representation is retained. Records are considered duplicates if their full key-value content is identical;
    /// field ordering within each dictionary may affect comparison results.
    /// </remarks>
    /// <param name="records">List of records to evaluate for duplicates; the original list is not modified.</param>
    /// <returns>
    /// A new <see cref="List{T}"/> containing only the first occurrence of each unique record, preserving the original
    /// order of non-duplicate entries.
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

    /// <summary>Normalizes a phone number string to a consistent 10-digit format.</summary>
    /// <remarks>
    /// Applies the following normalization rules in order:
    /// <list type="bullet">
    /// <item>
    /// If <paramref name="phoneNumber"/> is <see langword="null"/> or whitespace, it is returned as-is, substituting
    /// an empty string for <see langword="null"/>.
    /// </item>
    /// <item>All non-digit characters are stripped from the input string.</item>
    /// <item>
    /// If the resulting digit string is 11 characters long and begins with <c>1</c>, the leading digit is removed to
    /// strip the US country code.
    /// </item>
    /// <item>
    /// If the resulting digit string is exactly 10 characters, it is returned as the normalized value; otherwise, the
    /// original input is returned unchanged to avoid corrupting unrecognized formats.
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="phoneNumber">Phone number string to normalize; may be <see langword="null"/>.</param>
    /// <returns>
    /// A 10-digit string if normalization succeeds; the original <paramref name="phoneNumber"/> if the digit count
    /// does not equal 10 after stripping; or an empty string if the input is <see langword="null"/>.
    /// </returns>
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