// 260206_code
// 260206_documentation

using System.Data;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>
/// Helper routines for converting Excel worksheet <see cref="DataTable"/> instances into
/// in-memory structures used by the TeleHealth report processing pipeline.
/// </summary>
/// <remarks>
/// This static class contains a small set of focused routines that take a single worksheet (represented as
/// a <see cref="DataTable"/>) and transform its rows into one of several canonical shapes used by the rest of the
/// pipeline:
/// <list type="bullet">
///     <item>    Summary sheets - Simple key/value pairs aggregated across files</item>
///     <item>      Keyed sheets - Rows keyed by a unique column, optionally aggregating numeric fields</item>
///     <item>SimpleKeyed sheets - Keep first row per key</item>
///     <item>      Client stats - Multiple rows per client, grouped by client name</item>
///     <item>       Flat sheets - Preserve every row as a record</item>
/// </list>
/// The routines intentionally work with basic .NET types (dictionaries, lists and primitive values)
/// so the output can be serialized to JSON by <see cref="ReportUtility"/> without additional mapping.
/// </remarks>
internal static class ReportWorksheet
{
    /// <summary>
    /// Processes a summary-style worksheet that contains metric/value pairs.
    /// </summary>
    /// <param name="table">
    /// The <see cref="DataTable"/> representing the worksheet. The routine expects at least two columns where the
    /// first column is the metric name and the second column contains numeric or parseable numeric values.
    /// </param>
    /// <param name="metrics">
    /// A dictionary that will be updated with aggregated numeric values. Each metric name is used as the key. If a
    /// metric already exists in the dictionary its numeric value is incremented by the parsed value from here.
    /// </param>
    /// <param name="headers">
    /// Optional tuple used to capture the column header names from the first two columns. If <c>null</c> the method
    /// sets this to the names of the first two columns (falling back to "Metric" / "Value").
    /// </param>
    /// <remarks>
    /// - Rows with an empty or null metric name are ignored.<br/>
    /// - Values that cannot be parsed as doubles are treated as zero (see <see cref="ReportUtility.ParseDoubleValue(object?)"/>).<br/>
    /// - Calling this method repeatedly with the same tables will continue to accumulate totals in <paramref name="metrics"/>.
    /// </remarks>
    internal static void SummarySheet(DataTable table, Dictionary<string, double> metrics, ref (string, string)? headers)
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

            metrics[metricKey] =metrics.TryGetValue(metricKey, out var existingValue)
                ? existingValue + metricValue
                : metricValue;
        }
    }

    /// <summary>
    /// Processes worksheets where each row contains a unique identifier column and arbitrary additional columns.
    /// </summary>
    /// <param name="table">
    /// The <see cref="DataTable"/> representing the worksheet.
    /// </param>
    /// <param name="dataById">
    /// Output dictionary keyed by the string value found in the <paramref name="keyColumn"/>. Each value is a
    /// dictionary mapping column names to their cell value (object). Existing entries are merged or replaced.
    /// </param>
    /// <param name="headers">
    /// A mutable <see cref="List{T}"/> used to track ordered column headers encountered across processed tables. This
    /// list will be extended with new column names found in the given table.
    /// </param>
    /// <param name="keyColumn">
    /// The column name to use as the unique key for each row. Rows missing this column or with an empty key are
    /// ignored.
    /// </param>
    /// <param name="aggregateNumeric">
    /// When <c>true</c>, numeric columns are summed when duplicate keys are encountered. When <c>false</c>, the first
    /// observed row for a key is kept and subsequent rows are ignored for that key.
    /// </param>
    /// <remarks>
    /// - Uses <see cref="ReportUtility.UpdateHeaders(DataTable, ICollection{string})"/> to ensure <paramref name="headers"/>
    ///   contains all columns present in the table.<br/>
    /// - Builds row dictionaries with the capacity based on <paramref name="headers"/> to reduce reallocations.<br/>
    /// - When aggregation is requested, numeric merging uses <see cref="ReportUtility.MergeRows"/> which preserves
    ///   non-null values and sums numeric fields.
    /// </remarks>
    internal static void KeyedSheet(DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, List<string> headers, string keyColumn, bool aggregateNumeric = false)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        ReportUtility.UpdateHeaders(table, headers);

        foreach (DataRow dr in table.Rows)
        {
            var key = dr[keyColumn]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var row = ReportUtility.BuildRow(table, headers);

            foreach (var header in headers)
            {
                row[header] = table.Columns.Contains(header) ? dr[header] : null;
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

    /// <summary>
    /// Processes worksheets keyed by a unique column but keeps only the first row observed for each key.
    /// </summary>
    /// <param name="table">
    /// The <see cref="DataTable"/> representing the worksheet.
    /// </param>
    /// <param name="dataById">
    /// Output dictionary keyed by the string value found in the <paramref name="keyColumn"/>. Only the first row for
    /// each key is stored.
    /// </param>
    /// <param name="headers">
    /// A <see cref="HashSet{T}"/> used to track column headers across multiple tables. The set is updated with any new
    /// columns from the current table.
    /// </param>
    /// <param name="keyColumn">The name of the column that uniquely identifies rows. Rows with no key are ignored.</param>
    /// <remarks>
    /// - The method obtains the table's column names via <see cref="ReportUtility.UpdateHeaders(DataTable, ICollection{string})"/>
    ///   which returns a HashSet of the table's columns. The <paramref name="headers"/> set is then converted to an
    ///   ordered list so the produced row dictionaries preserve a stable ordering for consumers that rely on header
    ///   order.
    /// - If multiple rows share a key only the first encountered row is inserted into <paramref name="dataById"/>.
    /// </remarks>
    internal static void SimpleKeyedSheet(DataTable table, Dictionary<string, Dictionary<string, object?>> dataById, HashSet<string> headers, string keyColumn)
    {
        if (!table.Columns.Contains(keyColumn))
        {
            return;
        }

        var tableColumns   = ReportUtility.UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (DataRow dr in table.Rows)
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

    /// <summary>
    /// Processes worksheets that contain client-specific records, grouping multiple rows by the "Client Name" column.
    /// </summary>
    /// <param name="table">
    /// The <see cref="DataTable"/> representing the worksheet. The method expects a column named "Client Name".
    /// </param>
    /// <param name="statsByClient">
    /// Output dictionary mapping client name strings to a list of record dictionaries for that client. Each record is a
    /// dictionary of column name -> cell value and may include the "Client Name" field.
    /// </param>
    /// <param name="headers">
    /// A <see cref="HashSet{T}"/> of column headers that will be extended with columns found in the table.The
    /// resulting header set is used to produce uniform record dictionaries across clients.
    /// </param>
    /// <remarks>
    /// - Rows without a value in the "Client Name" column are ignored.<br/>
    /// - Each client key maps to a <see cref="List{T}"/> of dictionaries; callers should be prepared for clients with
    ///   zero, one or many records.<br/>
    /// - This method does not attempt to deduplicate client records; deduplication and serialization are handled by <see cref="ReportUtility"/>.
    /// </remarks>
    internal static void ClientStatsSheet(DataTable table, Dictionary<string, List<Dictionary<string, object?>>> statsByClient, HashSet<string> headers)
    {
        if (!table.Columns.Contains("Client Name"))
        {
            return;
        }

        var tableColumns = ReportUtility.UpdateHeaders(table, headers);
        var orderedHeaders = headers.ToList();

        foreach (DataRow dr in table.Rows)
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
                records = [];
                statsByClient[clientName] = records;
            }

            records.Add(row);
        }
    }

    /// <summary>
    /// Processes a worksheet as a flat list of records preserving every row.
    /// </summary>
    /// <param name="table">
    /// The <see cref="DataTable"/> representing the worksheet.
    /// </param>
    /// <param name="allRecords">
    /// A list that will receive one dictionary per row. Each dictionary maps column name -> cell value.
    /// </param>
    /// <param name="headers">
    /// A <see cref="HashSet{T}"/> that will be updated with all column names present in the table so that downstream
    /// consumers see a consistent set of headers across multiple flat tables.
    /// </param>
    /// <remarks>
    /// - The method captures the table's column names in their original order and then produces record dictionaries
    ///   that contain
    ///   values for those columns. Any header present in <paramref name="headers"/> but not in the current row will be
    ///   added to the row with a null value, ensuring all records share the same set of keys.<br/>
    /// - Use <see cref="ReportUtility.DeduplicateRecords"/> after collection if duplicate rows must be removed before
    ///   serialization.
    /// </remarks>
    internal static void FlatSheet(DataTable table, List<Dictionary<string, object?>> allRecords, HashSet<string> headers)
    {
        var tableColumns = new List<string>();

        foreach (DataColumn col in table.Columns)
        {
            var colName = col.ColumnName ?? string.Empty;
            tableColumns.Add(colName);
            headers.Add(colName);
        }

        var orderedHeaders = headers.ToList();

        foreach (DataRow dr in table.Rows)
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