// 260206_code
// 260206_documentation

/* I used Claude Sonnet 4.5 to help with working through Excel files, so some of this stuff is a little outside my
 * wheelhouse (e.g., ExcelDataReader, DataSet, DataTable, etc.). I've researched and commented things as best I can.
 */

using System.Data;
using System.IO;
using ExcelDataReader;

namespace TingenTransmorger.TeleHealthReport;

internal class ProcessWorkbook
{
    /// <summary>
    /// Excel data set configuration for reading Excel files.
    /// </summary>
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    /// <summary>
    /// Processes Message Delivery Excel reports.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Temporary data directory.
    /// </param>
    /// <param name="statusCallback">
    /// Optional callback to report status messages.
    /// </param>
    internal static void MessageDelivery(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        var allRecords = new List<Dictionary<string, object?>>();
        var headers    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process(importDir, "*Message_Delivery*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Stats", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.Flat(worksheet, allRecords, headers);
            }
        }, statusCallback);

        ReportUtility.WriteFlatJson(tmpDir, "Message_Delivery-Message_Delivery_Stats.json", allRecords);
    }

    /// <summary>
    /// Processes Message Failure Excel reports.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Temporary data directory.
    /// </param>
    /// <param name="statusCallback">
    /// Optional callback to report status messages.
    /// </param>
    internal static void MessageFailure(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        (string, string)? summaryHeaders = null;

        var smsStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var smsStatsHeaders  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var emailStatsByClient = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var emailStatsHeaders  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process(importDir, "*Message_Failure*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.Summary(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("SMS STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ClientStats(worksheet, smsStatsByClient, smsStatsHeaders);
            }
            else if (sheetName.Equals("EMAIL STATS", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.ClientStats(worksheet, emailStatsByClient, emailStatsHeaders);
            }
        }, statusCallback);

        ReportUtility.WriteSummaryJson(tmpDir, "Message_Failure-Summary.json", summaryMetrics, summaryHeaders);
        ReportUtility.WriteClientStatsJson(tmpDir, "Message_Failure-Sms_Stats.json", smsStatsByClient);
        ReportUtility.WriteClientStatsJson(tmpDir, "Message_Failure-Email_Stats.json", emailStatsByClient);
    }

    /// <summary>
    /// Processes Visit Detail Excel reports.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Temporary data directory.
    /// </param>
    /// <param name="statusCallback">
    /// Optional callback to report status messages.
    /// </param>
    internal static void VisitDetails(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        var meetingDetailsById    = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var participantDetails        = new List<Dictionary<string, object?>>();
        var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process(importDir, "*Visit_Details*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Meeting Details", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.SimpleKeyed(worksheet, meetingDetailsById, meetingDetailsHeaders, "Meeting ID");
            }
            else if (sheetName.Equals("Participant Details", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.Flat(worksheet, participantDetails, participantDetailsHeaders);
            }
        }, statusCallback);

        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Details-Meeting_Details.json", meetingDetailsById);
        ReportUtility.WriteFlatJson(tmpDir, "Visit_Details-Participant_Details.json", participantDetails);
    }

    /// <summary>
    /// Processes Visit Stats Excel reports.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Temporary data directory.
    /// </param>
    /// <param name="statusCallback">
    /// Optional callback to report status messages.
    /// </param>
    internal static void VisitStats(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        (string, string)? summaryHeaders = null;

        var meetingErrorsById   = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingErrorHeaders = new List<string>();

        Process(importDir, "*Visit_Stats*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.Summary(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("Meeting Errors", StringComparison.OrdinalIgnoreCase))
            {
                ProcessWorksheet.Keyed(worksheet, meetingErrorsById, meetingErrorHeaders, "Meeting ID", aggregateNumeric: true);
            }
        }, statusCallback);

        ReportUtility.WriteSummaryJson(tmpDir, "Visit_Stats-Summary.json", summaryMetrics, summaryHeaders);
        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Stats-Meeting_Errors.json", meetingErrorsById);
    }

    /// <summary>Processes Excel files matching a pattern and invokes a callback for each worksheet.</summary>
    /// <param name="importDir">Directory to search for Excel files.</param>
    /// <param name="pattern">File search pattern (e.g., "*Visit_Stats*.xlsx").</param>
    /// <param name="processSheet">Callback action that receives each DataTable and sheet name.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    private static void Process(string importDir, string pattern, Action<DataTable, string> processSheet, Action<string>? statusCallback = null)
    {
        string[] matchingFiles = Directory.GetFiles(importDir, pattern, SearchOption.TopDirectoryOnly);
        int processedCount = 0;
        int totalFiles = matchingFiles.Length;

        foreach (string filePath in matchingFiles)
        {
            // Skip Excel temporary files (lock files that start with ~$)
            string fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("~$"))
            {
                continue;
            }

            statusCallback?.Invoke($"Processing file {processedCount + 1}/{totalFiles}: {fileName}");

            using FileStream fileStream        = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(fileStream);

            DataSet dataSet = excelReader.AsDataSet(ExcelConfig);

            foreach (DataTable worksheet in dataSet.Tables)
            {
                if (worksheet.Columns.Count > 0)
                {
                    processSheet(worksheet, worksheet.TableName);
                }
            }

            processedCount++;
        }
    }
}