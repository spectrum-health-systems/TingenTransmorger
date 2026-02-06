// 260206_code
// 260206_documentation

using System.IO;
using System.Text;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>
/// Entry class for processing TeleHealth reports.
/// </summary>
/// <remarks>
/// <para>
/// Processes TeleHealth Excel reports and converts them to JSON format.
/// </para>
/// <para>
/// This processor handles four types of reports:
/// <list type="bullet">
///     <item>Visit Stats - Summary and Meeting Errors</item>
///     <item>Visit Details - Meeting Details and Participant Details</item>
///     <item>Message Failure - Summary, SMS Stats, and Email Stats</item>
///     <item>Message Delivery - Message Delivery Stats</item>
/// </list>
/// </para>
/// </remarks>
static class ReportProcessor
{
    /// <summary>
    /// Processes all TeleHealth reports from the import directory and generates JSON output files.
    /// </summary>
    /// <param name="config">
    /// Configuration object containing import and output directory paths.
    /// </param>
    internal static void Process(string importDir, string tmpDir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);

        ProcessWorkbook.VisitStats(importDir, tmpDir);
        ProcessWorkbook.VisitDetails(importDir, tmpDir);
        ProcessWorkbook.MessageFailure(importDir, tmpDir);
        ProcessWorkbook.MessageDelivery(importDir, tmpDir);
    }
}