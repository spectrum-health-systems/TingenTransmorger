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

    /// <summary>
    /// Processes Visit Stats reports with progress callback support.
    /// </summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessVisitStats(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.VisitStats(importDir, tmpDir, statusCallback);
    }

    /// <summary>
    /// Processes Visit Details reports with progress callback support.
    /// </summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessVisitDetails(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.VisitDetails(importDir, tmpDir, statusCallback);
    }

    /// <summary>
    /// Processes Message Failure reports with progress callback support.
    /// </summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessMessageFailure(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.MessageFailure(importDir, tmpDir, statusCallback);
    }

    /// <summary>
    /// Processes Message Delivery reports with progress callback support.
    /// </summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessMessageDelivery(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.MessageDelivery(importDir, tmpDir, statusCallback);
    }
}