// 260311_code
// 260311_documentation

using System.Data;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Provides methods for processing individual worksheets from TeleHealth report workbooks.</summary>
internal static class ProcessWorksheet
{
    /// <summary>Processes a summary-style worksheet, accumulating key-value metric pairs into a dictionary.</summary>
    /// <remarks>
    /// <para>
    /// Expects a two-column <see cref="DataTable"/> where the first column contains metric names and the second
    /// contains numeric values. Rows with a <c>null</c> or empty metric key are skipped.
    /// </para>
    /// <para>
    /// If <paramref name="headers"/> is <c>null</c>, it is initialized from the table's column names, falling back to
    /// <b>Metric</b> and <b>Value</b> if names are unavailable.
    /// </para>
    /// <para>Duplicate metric keys are summed rather than overwritten.</para>
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> containing at least two columns.</param>
    /// <param name="metrics">Dictionary accumulating metric name/value pairs across one or more worksheets.</param>
    /// <param name="headers">
    /// Column header tuple <c>(metricColumn, valueColumn)</c>; initialized from <paramref name="table"/> on the first
    /// call if <c>null</c>.
    /// </param>
    internal static void Summary(DataTable table, Dictionary<string, double> metrics, ref (string, string)? headers)
    {
        if (table.Columns.Count < 2)
        {
            return;
        }

        headers ??= (table.Columns[0].ColumnName ?? "Metric", table.Columns[1].ColumnName ?? "Value");

        foreach (DataRow dataRow in table.Rows)
        {
            var metricKey = dataRow[0]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(metricKey))
            {
                continue;
            }

            var metricValue = ReportUtility.ParseDoubleValue(dataRow[1]);

            metrics[metricKey] = metrics.TryGetValue(metricKey, out var existingValue)
                ? existingValue + metricValue
                : metricValue;
        }
    }

    /// <summary>Processes a keyed worksheet, building a dictionary of row data indexed by a specified key column.</summary>
    /// <remarks>
    /// <para>
    /// If <paramref name="aggregateNumeric"/> is <c>true</c>, numeric values in duplicate-key rows are summed into the
    /// existing entry via <see cref="ReportUtility.MergeRows"/>; otherwise, only the first occurrence of each key is
    /// retained.
    /// </para>
    /// <para>Rows with a <c>null</c> or empty key value are skipped.</para>
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> containing the report data.</param>
    /// <param name="dataById">Dictionary to populate with row data keyed by <paramref name="keyColumn"/> values.</param>
    /// <param name="headers">Ordered list of column headers to include; updated with any new columns from <paramref name="table"/>.</param>
    /// <param name="keyColumn">Name of the column whose value is used as the row key.</param>
    /// <param name="aggregateNumeric">
    /// When <c>true</c>, numeric values for duplicate keys are summed; when <c>false</c>, duplicate keys are ignored.
    /// </param>
    internal static void Keyed(DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, List<string> headers, string keyColumn, bool aggregateNumeric = false)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        ReportUtility.UpdateHeaders(table, headers);

        foreach (DataRow dataRow in table.Rows)
        {
            var key = dataRow[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var row = ReportUtility.BuildRow(table, headers);

            foreach (var header in headers)
            {
                row[header] = table.Columns.Contains(header) ? dataRow[header] : null;
            }

            if (dataById.TryGetValue(key, out var existingRow) && aggregateNumeric)
            {
                ReportUtility.MergeRows(existingRow, row, keyColumn);
            }
            else
            {
                dataById.TryAdd(key, row);
            }
        }
    }

    /// <summary>Processes a keyed worksheet, retaining only the first row encountered for each unique key value.</summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="Keyed"/>, this method uses a <see cref="HashSet{T}"/> for header tracking and does not
    /// aggregate numeric values — duplicate keys are silently ignored.
    /// </para>
    /// <para>Rows with a <c>null</c> or empty key value are skipped.</para>
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> containing the report data.</param>
    /// <param name="dataById">Dictionary to populate with row data keyed by <paramref name="keyColumn"/> values.</param>
    /// <param name="headers">Set of column headers; updated with any new columns from <paramref name="table"/>.</param>
    /// <param name="keyColumn">Name of the column whose value is used as the row key.</param>
    internal static void SimpleKeyed(DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, HashSet<string> headers, string keyColumn)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        var tableColumns   = ReportUtility.UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (DataRow dataRow in table.Rows)
        {
            var key = dataRow[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!dataById.ContainsKey(key))
            {
                var row = new Dictionary<string, object?>(orderedHeaders.Count);

                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dataRow[header] : null;
                }

                dataById[key] = row;
            }
        }
    }

    /// <summary>Processes a worksheet containing per-client statistics, grouping rows by client name.</summary>
    /// <remarks>
    /// <para>
    /// Requires a column named <b>Client Name</b>; the method returns without processing if that column is absent from
    /// <paramref name="table"/>.
    /// </para>
    /// <para>
    /// Each row is appended to the list associated with its client name in <paramref name="statsByClient"/>. A new list
    /// is created automatically for first-seen client names.
    /// </para>
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> that must contain a <b>Client Name</b> column.</param>
    /// <param name="statsByClient">Dictionary mapping client names to their accumulated list of row records.</param>
    /// <param name="headers">Set of column headers; updated with any new columns from <paramref name="table"/>.</param>
    internal static void ClientStats(DataTable table, Dictionary<string, List<Dictionary<string, object?>>> statsByClient, HashSet<string> headers)
    {
        if (!table.Columns.Contains("Client Name"))
        {
            return;
        }

        var tableColumns = ReportUtility.UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (DataRow dataRow in table.Rows)
        {
            var clientName = dataRow["Client Name"]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(clientName))
            {
                continue;
            }

            var row = new Dictionary<string, object?>(orderedHeaders.Count);

            foreach (var header in orderedHeaders)
            {
                row[header] = tableColumns.Contains(header) ? dataRow[header] : null;
            }

            if (!statsByClient.TryGetValue(clientName, out var records))
            {
                records = [];
                statsByClient[clientName] = records;
            }

            records.Add(row);
        }
    }

    /// <summary>Processes a worksheet into a flat list of row dictionaries, null-filling any missing headers.</summary>
    /// <remarks>
    /// <para>
    /// All column names from <paramref name="table"/> are added to <paramref name="headers"/>, enabling a consistent
    /// column set to be maintained across multiple worksheets.
    /// </para>
    /// <para>
    /// For each row, columns present in the table are populated with their values; any headers in
    /// <paramref name="headers"/> not found in the current table are set to <c>null</c>.
    /// </para>
    /// </remarks>
    /// <param name="table">Source <see cref="DataTable"/> containing the report data.</param>
    /// <param name="allRecords">List to which all processed row dictionaries are appended.</param>
    /// <param name="headers">Set of all column headers encountered across worksheets; updated with columns from <paramref name="table"/>.</param>
    internal static void Flat(DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
    {
        var tableColumns = new List<string>();

        foreach (DataColumn column in table.Columns)
        {
            var columnName = column.ColumnName ?? string.Empty;
            tableColumns.Add(columnName);
            headers.Add(columnName);
        }

        var orderedHeaders = headers.ToList();

        foreach (DataRow dataRow in table.Rows)
        {
            var row = new Dictionary<string, object?>(orderedHeaders.Count);

            for (int columnIndex = 0; columnIndex < tableColumns.Count; columnIndex++)
            {
                row[tableColumns[columnIndex]] = dataRow[columnIndex];
            }

            foreach (var header in orderedHeaders.Where(header => !row.ContainsKey(header)))
            {
                row[header] = null;
            }

            allRecords.Add(row);
        }
    }
}