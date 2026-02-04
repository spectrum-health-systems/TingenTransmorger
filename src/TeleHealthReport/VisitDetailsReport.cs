using System.IO;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using TingenTransmorger.Core;

namespace TingenTransmorger.TeleHealthReport
{
    class VisitDetailsReport
    {
        internal static void Adder(Configuration config)
        {
            if (config == null)
                return;

            // Register encoding provider required by ExcelDataReader
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!config.AdminDirectories.TryGetValue("Import", out var importDir) || string.IsNullOrEmpty(importDir))
            {
                return; // nothing to do
            }

            if (!config.AdminDirectories.TryGetValue("Tmp", out var tmpDir) || string.IsNullOrEmpty(tmpDir))
            {
                return; // no tmp location
            }

            if (!Directory.Exists(importDir))
                return;

            Directory.CreateDirectory(tmpDir);

            // Meeting Details aggregation: keyed by Meeting ID
            var meetingDetailsById = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            var meetingDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Participant Details aggregation: keyed by Participant Name
            var participantDetailsByName = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var files = Directory.GetFiles(importDir, "*Visit_Details*.xlsx", SearchOption.TopDirectoryOnly);

            var excelConfig = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            };

            foreach (var file in files)
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet(excelConfig);

                foreach (System.Data.DataTable table in dataSet.Tables)
                {
                    if (table.Columns.Count == 0)
                        continue;

                    var sheetName = table.TableName;

                    // Process Meeting Details sheet
                    if (sheetName.Equals("Meeting Details", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessMeetingDetailsSheet(table, meetingDetailsById, meetingDetailsHeaders);
                    }
                    // Process Participant Details sheet
                    else if (sheetName.Equals("Participant Details", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessParticipantDetailsSheet(table, participantDetailsByName, participantDetailsHeaders);
                    }
                }
            }

            // Write Meeting Details JSON
            WriteMeetingDetailsJson(tmpDir, meetingDetailsById);

            // Write Participant Details JSON
            WriteParticipantDetailsJson(tmpDir, participantDetailsByName);
        }

        private static void ProcessMeetingDetailsSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> meetingDetailsById, HashSet<string> meetingDetailsHeaders)
        {
            if (table.Columns.Count == 0)
                return;

            // Check if Meeting ID column exists in current table
            if (!table.Columns.Contains("Meeting ID"))
                return;

            // Build column name cache and update headers set
            var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Data.DataColumn col in table.Columns)
            {
                var colName = col.ColumnName ?? string.Empty;
                tableColumns.Add(colName);
                meetingDetailsHeaders.Add(colName);
            }

            // Get ordered list of headers for consistent output
            var orderedHeaders = meetingDetailsHeaders.ToList();

            foreach (System.Data.DataRow dr in table.Rows)
            {
                var meetingId = dr["Meeting ID"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(meetingId))
                    continue;

                // Build row dictionary
                var row = new Dictionary<string, object?>(orderedHeaders.Count);
                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dr[header] : null;
                }

                // If Meeting ID already exists, keep the first occurrence or merge based on business rules
                // For now, keeping first occurrence (remove if to overwrite with latest)
                if (!meetingDetailsById.ContainsKey(meetingId))
                {
                    meetingDetailsById[meetingId] = row;
                }
            }
        }

        private static void ProcessParticipantDetailsSheet(System.Data.DataTable table, Dictionary<string, Dictionary<string, object?>> participantDetailsByName, HashSet<string> participantDetailsHeaders)
        {
            if (table.Columns.Count == 0)
                return;

            // Check if Participant Name column exists in current table
            if (!table.Columns.Contains("Participant Name"))
                return;

            // Build column name cache and update headers set
            var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Data.DataColumn col in table.Columns)
            {
                var colName = col.ColumnName ?? string.Empty;
                tableColumns.Add(colName);
                participantDetailsHeaders.Add(colName);
            }

            // Get ordered list of headers for consistent output
            var orderedHeaders = participantDetailsHeaders.ToList();

            foreach (System.Data.DataRow dr in table.Rows)
            {
                var participantName = dr["Participant Name"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(participantName))
                    continue;

                // Build row dictionary
                var row = new Dictionary<string, object?>(orderedHeaders.Count);
                foreach (var header in orderedHeaders)
                {
                    row[header] = tableColumns.Contains(header) ? dr[header] : null;
                }

                // If Participant Name already exists, keep the first occurrence or merge based on business rules
                // For now, keeping first occurrence (remove if to overwrite with latest)
                if (!participantDetailsByName.ContainsKey(participantName))
                {
                    participantDetailsByName[participantName] = row;
                }
            }
        }

        private static void WriteMeetingDetailsJson(string tmpDir, Dictionary<string, Dictionary<string, object?>> meetingDetailsById)
        {
            if (meetingDetailsById.Count == 0)
                return;

            var meetingDetailsRows = new List<Dictionary<string, object?>>(meetingDetailsById.Values);
            var outputPath = Path.Combine(tmpDir, "Visit_Details-Meeting_Details.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(meetingDetailsRows, options);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }

        private static void WriteParticipantDetailsJson(string tmpDir, Dictionary<string, Dictionary<string, object?>> participantDetailsByName)
        {
            if (participantDetailsByName.Count == 0)
                return;

            var participantDetailsRows = new List<Dictionary<string, object?>>(participantDetailsByName.Values);
            var outputPath = Path.Combine(tmpDir, "Visit_Details-Participant_Details.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(participantDetailsRows, options);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }
    }
}
