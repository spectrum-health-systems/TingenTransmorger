// 260206_code
// 260206_documentation

namespace TingenTransmorger.TeleHealthReport;

internal static class VisitDetailReport
{
    /// <summary>Processes Visit Details reports containing meeting and participant information.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Directory where JSON output files will be written.</param>
    internal static void ProcessVisitDetailsReports(string importDir, string tmpDir)
    {
        var meetingDetailsById    = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var participantDetails        = new List<Dictionary<string, object?>>();
        var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ReportProcessor.ProcessExcelFiles(importDir, "*Visit_Details*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Meeting Details", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.SimpleKeyedSheet(worksheet, meetingDetailsById, meetingDetailsHeaders, "Meeting ID");
            }
            else if (sheetName.Equals("Participant Details", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.FlatSheet(worksheet, participantDetails, participantDetailsHeaders);
            }
        });

        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Details-Meeting_Details.json", meetingDetailsById);
        ReportUtility.WriteFlatJson(tmpDir, "Visit_Details-Participant_Details.json", participantDetails);
    }
}