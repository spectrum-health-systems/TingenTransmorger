// 260205_code
// 260205_documentation

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

        var participantDetailsByName  = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ReportProcessor.ProcessExcelFiles(importDir, "*Visit_Details*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Meeting Details", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.SimpleKeyedSheet(worksheet, meetingDetailsById, meetingDetailsHeaders, "Meeting ID");
            }
            else if (sheetName.Equals("Participant Details", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.SimpleKeyedSheet(worksheet, participantDetailsByName, participantDetailsHeaders, "Participant Name");
            }
        });

        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Details-Meeting_Details.json", meetingDetailsById);
        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Details-Participant_Details.json", participantDetailsByName);
    }
}