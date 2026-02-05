// 260205_code
// 260205_documentation

namespace TingenTransmorger.TeleHealthReport;

internal class MessageFailureReport
{
    /// <summary>Processes Message Failure reports containing delivery summaries and client statistics.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Directory where JSON output files will be written.</param>
    internal static void ProcessMessageFailureReports(string importDirectory, string temporaryDirectory)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        (string, string)? summaryHeaders = null;

        var smsStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var smsStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var emailStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var emailStatsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ReportProcessor.ProcessExcelFiles(importDirectory, "*Message_Failure*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ProcessSummarySheet(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("SMS STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ProcessClientStatsSheet(worksheet, smsStatsByClient, smsStatsHeaders);
            }
            else if (sheetName.Equals("EMAIL STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ProcessClientStatsSheet(worksheet, emailStatsByClient, emailStatsHeaders);
            }
        });

        ReportProcessor.WriteSummaryJson(temporaryDirectory, "Message_Failure-Summary.json", summaryMetrics, summaryHeaders);
        ReportProcessor.WriteClientStatsJson(temporaryDirectory, "Message_Failure-SMS_Stats.json", smsStatsByClient);
        ReportProcessor.WriteClientStatsJson(temporaryDirectory, "Message_Failure-Email_Stats.json", emailStatsByClient);
    }
}
