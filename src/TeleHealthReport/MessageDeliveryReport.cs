// 260206_code
// 260206_documentation

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Logic specific to the Message Delivery Excel reports.</summary>
internal static class MessageDeliveryReport
{
    /// <summary>Processes Message Delivery reports containing delivery statistics.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Directory where JSON output files will be written.</param>
    internal static void ProcessMessageDeliveryReports(string importDir, string tmpDir)
    {
        var allRecords = new List<Dictionary<string, object?>>();
        var headers    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ReportProcessor.ProcessExcelFiles(importDir, "*Message_Delivery*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Stats", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.FlatSheet(worksheet, allRecords, headers);
            }
        });

        ReportUtility.WriteFlatJson(tmpDir, "Message_Delivery-Message_Delivery_Stats.json", allRecords);
    }
}