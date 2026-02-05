// 260205_code
// 260205_documentation

namespace TingenTransmorger.TeleHealthReport;

internal class VisitStatsReport
{
    /// <summary>Processes Visit Stats reports containing summary metrics and meeting errors.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Directory where JSON output files will be written.</param>
    internal static void ProcessVisitStatsReports(string importDir, string tmpDir)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        (string, string)? summaryHeaders = null;

        var meetingErrorsById = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingErrorHeaders = new List<string>();

        ReportProcessor.ProcessExcelFiles(importDir, "*Visit_Stats*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ProcessSummarySheet(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("Meeting Errors", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ProcessKeyedSheet(worksheet, meetingErrorsById, meetingErrorHeaders, "Meeting ID", aggregateNumeric: true);
            }
        });

        ReportProcessor.WriteSummaryJson(tmpDir, "Visit_Stats-Summary.json", summaryMetrics, summaryHeaders);
        ReportProcessor.WriteKeyedJson(tmpDir, "Visit_Stats-Meeting_Errors.json", meetingErrorsById);
    }
}