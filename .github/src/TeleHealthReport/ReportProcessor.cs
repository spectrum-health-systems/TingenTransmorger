// 260206_code
// 260206_documentation

using System.IO;
using System.Text;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>
/// This is the entry class for processing and converting TeleHealth reports.
/// </summary>
/// <remarks>
/// TeleHealth reports are provided as Excel files, and downloaded individually. Those reports are then converted into
/// various JSON files that are used to build the final Transmorger database.<br/>
/// <br/>
/// This class orchestrates the overall process of reading Excel files from a specified import directory, processing
/// them according to their type, and outputting JSON files to a temporary directory for later use in building the
/// Transmorger database.<br/>
/// <br/>
/// The four types of reports that are processed are:<br/>
/// <list type="bullet">
///     <item>     Visit Stats - Summary and Meeting Errors</item>
///     <item>   Visit Details - Meeting Details and Participant Details</item>
///     <item> Message Failure - Summary, SMS Stats, and Email Stats</item>
///     <item>Message Delivery - Message Delivery Stats</item>
/// </list>
/// </remarks>
static class ReportProcessor
{
    /// <summary>
    /// This is the entry method for processing and converting TeleHealth reports.
    /// </summary>
    /// <param name="importDir">
    /// The directory path where the Excel report files are located.
    /// </param>
    /// <param name="tmpDir">
    /// Temporary data
    /// </param>
    internal static void Process(string importDir, string tmpDir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Start with a fresh temporary data directory.
        if (Directory.Exists(tmpDir))
        {
            Directory.Delete(tmpDir, recursive: true);
        }

        Directory.CreateDirectory(tmpDir);

        ExcelFile.VisitStats(importDir, tmpDir);
        ExcelFile.VisitDetails(importDir, tmpDir);
        ExcelFile.MessageFailure(importDir, tmpDir);
        ExcelFile.MessageDelivery(importDir, tmpDir);
    }
}