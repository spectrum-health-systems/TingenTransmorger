// 260205_code
// 260205_documentation

namespace TingenTransmorger.TeleHealthReport;

internal class ProcessWorksheet
{
    /// <summary>Processes summary sheets with key-value pairs, aggregating numeric values across files.</summary>
    /// <param name="table">DataTable containing the summary sheet data.</param>
    /// <param name="metrics">Dictionary to store aggregated metrics.</param>
    /// <param name="headers">Optional tuple to capture column header names.</param>
    internal static void ProcessSummarySheet(System.Data.DataTable table, Dictionary<string, double> metrics, ref (string, string)? headers)
    {
        if (table.Columns.Count < 2)
        {
            return;
        }

        headers ??= (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");

        foreach (System.Data.DataRow dataRow in table.Rows)
        {
            var metricKey = dataRow[0]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(metricKey))
            {
                continue;
            }

            var metricValue = ReportProcessor.ParseDoubleValue(dataRow[1]);

            if (metrics.TryGetValue(metricKey, out var existingValue))
            {
                metrics[metricKey] = existingValue + metricValue;
            }
            else
            {
                metrics[metricKey] = metricValue;
            }
        }
    }

    /// <summary>Processes sheets with a unique key column, optionally aggregating numeric values for duplicate keys.</summary>
    /// <param name="table">DataTable containing the sheet data.</param>
    /// <param name="dataById">Dictionary to store records keyed by the specified column.</param>
    /// <param name="headers">List to track all column headers encountered.</param>
    /// <param name="keyColumn">Name of the column to use as the unique key.</param>
    /// <param name="aggregateNumeric">If true, numeric values are summed for duplicate keys.</param>
    internal static void ProcessKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById,
        List<string> headers, string keyColumn, bool aggregateNumeric = false)
    {
        if (!table.Columns.Contains(keyColumn))
            return;

        ReportProcessor.UpdateHeaders(table, headers);

        foreach (System.Data.DataRow dr in table.Rows)
        {
            var key = dr[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var row = ReportProcessor.BuildRow(table, headers);

            foreach (var header in headers)
            {
                row[header] = table.Columns.Contains(header) ? dr[header] : null;
            }

            if (dataById.TryGetValue(key, out var existingRow) && aggregateNumeric)
            {
                ReportProcessor.MergeRows(existingRow, row, keyColumn);
            }
            else
            {
                dataById.TryAdd(key, row);
            }
        }
    }

    /// <summary>Processes sheets with a unique key column, keeping only the first occurrence of each key.</summary>
    /// <param name="table">DataTable containing the sheet data.</param>
    /// <param name="dataById">Dictionary to store records keyed by the specified column.</param>
    /// <param name="headers">HashSet to track all column headers encountered.</param>
    /// <param name="keyColumn">Name of the column to use as the unique key.</param>
    internal static void ProcessSimpleKeyedSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, HashSet<string> headers, string keyColumn)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        var tableColumns = ReportProcessor.UpdateHeaders(table, headers);
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

    /// <summary>Processes sheets with client statistics, allowing multiple records per client.</summary>
    /// <param name="table">DataTable containing the sheet data.</param>
    /// <param name="statsByClient">Dictionary to store lists of records per client.</param>
    /// <param name="headers">HashSet to track all column headers encountered.</param>
    internal static void ProcessClientStatsSheet(System.Data.DataTable table, Dictionary<string, List<Dictionary<string, object?>>> statsByClient, HashSet<string> headers)
    {
        if (!table.Columns.Contains("Client Name"))
        {
            return;
        }

        var tableColumns = ReportProcessor.UpdateHeaders(table, headers);
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

    /// <summary>Processes sheets as flat record lists, capturing all rows without keying or aggregation.</summary>
    /// <param name="table">DataTable containing the sheet data.</param>
    /// <param name="allRecords">List to store all records.</param>
    /// <param name="headers">HashSet to track all column headers encountered.</param>
    internal static void ProcessFlatSheet(System.Data.DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
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
}
