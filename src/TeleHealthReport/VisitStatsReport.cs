// 260204_code
// 260204_documentation

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using TingenTransmorger.Core;

namespace TingenTransmorger.TeleHealthReport;

class VisitStatsReport
{
    internal static void Adder(Configuration config)
    {
        // Register encoding provider required by ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var importDir = config.AdminDirectories["Import"];
        var tmpDir = config.AdminDirectories["Tmp"];

        // Summary aggregation: key/value pairs with summed values
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        (string, string)? summaryHeaders = null;

        // Meeting Errors aggregation: keyed by Meeting ID
        var meetingErrorsById   = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingErrorHeaders = new List<string>();

        var files = Directory.GetFiles(importDir, "*Visit_Stats*.xlsx", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var conf = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };

            var dataSet = reader.AsDataSet(conf);

            foreach (System.Data.DataTable table in dataSet.Tables)
            {
                if (table.Columns.Count == 0)
                    continue;

                var sheetName = table.TableName;

                // Process Summary sheet
                if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessSummarySheet(table, summaryMetrics, ref summaryHeaders);
                }
                // Process Meeting Errors sheet
                else if (sheetName.Equals("Meeting Errors", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessMeetingErrorsSheet(table, meetingErrorsById, meetingErrorHeaders);
                }
                // Ignore Missed Visit Stats and other sheets
            }
        }

        // Write Summary JSON
        if (summaryMetrics.Count > 0 && summaryHeaders != null)
        {
            var summaryRows = new List<Dictionary<string, object?>>();
            foreach (var kv in summaryMetrics)
            {
                var dict = new Dictionary<string, object?>
                {
                    [summaryHeaders.Value.Item1] = kv.Key,
                    [summaryHeaders.Value.Item2] = kv.Value
                };
                summaryRows.Add(dict);
            }

            var summaryPath = Path.Combine(tmpDir, "Visit_Stats-Summary.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var summaryJson = JsonSerializer.Serialize(summaryRows, options);
            File.WriteAllText(summaryPath, summaryJson, Encoding.UTF8);
        }

        // Write Meeting Errors JSON
        if (meetingErrorsById.Count > 0)
        {
            var meetingErrorRows = new List<Dictionary<string, object?>>(meetingErrorsById.Values);

            var meetingErrorPath = Path.Combine(tmpDir, "Visit_Stats-Meeting_Errors.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var meetingErrorJson = JsonSerializer.Serialize(meetingErrorRows, options);
            File.WriteAllText(meetingErrorPath, meetingErrorJson, Encoding.UTF8);
        }
    }

    private static void ProcessSummarySheet(System.Data.DataTable table, Dictionary<string, double> summaryMetrics, ref (string, string)? summaryHeaders)
    {
        if (table.Columns.Count < 2)
            return;

        // Capture headers
        if (summaryHeaders == null)
        {
            summaryHeaders = (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");
        }

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var keyObj = dr[0];
            var valObj = dr[1];
            var key = keyObj?.ToString()?.Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            // Try parse numeric value
            double value;
            if (valObj == null || string.IsNullOrEmpty(valObj.ToString()))
            {
                value = 0.0;
            }
            else if (!double.TryParse(valObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                double.TryParse(valObj.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out value);
            }

            if (summaryMetrics.ContainsKey(key))
                summaryMetrics[key] += value;
            else
                summaryMetrics[key] = value;
        }
    }

    private static void ProcessMeetingErrorsSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> meetingErrorsById, List<string> meetingErrorHeaders)
    {
        if (table.Columns.Count == 0)
            return;

        // Build headers (capture once)
        if (meetingErrorHeaders.Count == 0)
        {
            foreach (System.Data.DataColumn c in table.Columns)
            {
                meetingErrorHeaders.Add(c.ColumnName ?? string.Empty);
            }
        }
        else
        {
            // Add any new columns from this table that aren't in the headers yet
            foreach (System.Data.DataColumn c in table.Columns)
            {
                var colName = c.ColumnName ?? string.Empty;
                if (!meetingErrorHeaders.Contains(colName))
                {
                    meetingErrorHeaders.Add(colName);
                }
            }
        }

        // Check if Meeting ID column exists in current table
        if (!table.Columns.Contains("Meeting ID"))
            return;

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var meetingIdObj = dr["Meeting ID"];
            var meetingId = meetingIdObj?.ToString()?.Trim();
            if (string.IsNullOrEmpty(meetingId))
                continue;

            // Build row dictionary
            var row = new Dictionary<string, object?>();
            foreach (var header in meetingErrorHeaders)
            {
                // Only access columns that exist in this table
                if (table.Columns.Contains(header))
                {
                    row[header] = dr[header];
                }
                else
                {
                    row[header] = null; // Column doesn't exist in this file
                }
            }

            // If Meeting ID already exists, aggregate numeric values
            if (meetingErrorsById.ContainsKey(meetingId))
            {
                var existingRow = meetingErrorsById[meetingId];
                foreach (var header in meetingErrorHeaders)
                {
                    if (header.Equals("Meeting ID", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var existingVal = existingRow.TryGetValue(header, out var exVal) ? exVal : null;
                    var newVal = row.TryGetValue(header, out var nVal) ? nVal : null;

                    // Attempt to add numeric values
                    if (TryParseDouble(existingVal, out var existingNum) && TryParseDouble(newVal, out var newNum))
                    {
                        existingRow[header] = existingNum + newNum;
                    }
                    else if (newVal != null && existingVal == null)
                    {
                        // New value exists but existing doesn't, set it
                        existingRow[header] = newVal;
                    }
                    // Otherwise keep existing value (or concatenate if needed)
                }
            }
            else
            {
                meetingErrorsById[meetingId] = row;
            }
        }
    }

    private static bool TryParseDouble(object? val, out double result)
    {
        result = 0.0;
        if (val == null)
            return false;
        var str = val.ToString();
        if (string.IsNullOrEmpty(str))
            return false;

        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return true;

        return double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }
}