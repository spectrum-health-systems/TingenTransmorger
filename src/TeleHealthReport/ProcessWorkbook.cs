// 260311_code
// 260311_documentation

using System.Data;
using System.IO;
using ExcelDataReader;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Provides methods for processing TeleHealth report Excel workbooks into JSON output files.</summary>
internal static class ProcessWorkbook
{
    /// <summary>Excel dataset configuration that treats the first row of each sheet as a header row.</summary>
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    /// <summary>Processes Message Delivery Excel workbooks, writing a flat JSON report of delivery statistics.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory for JSON output.</param>
    /// <param name="statusCallback">Optional callback to report progress messages.</param>
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

    /// <summary>Processes Message Failure Excel workbooks, writing summary, SMS statistics, and email statistics JSON reports.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory for JSON output.</param>
    /// <param name="statusCallback">Optional callback to report progress messages.</param>
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

    /// <summary>Processes Visit Details Excel workbooks, writing meeting details and participant details JSON reports.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory for JSON output.</param>
    /// <param name="statusCallback">Optional callback to report progress messages.</param>
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

    /// <summary>Processes Visit Statistics Excel workbooks, writing summary and meeting errors JSON reports.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory for JSON output.</param>
    /// <param name="statusCallback">Optional callback to report progress messages.</param>
    internal static void VisitStats(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        var summaryMetrics        = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
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

    /// <summary>Processes all Excel files in a directory that match a given pattern, invoking a handler for each worksheet.</summary>
    /// <remarks>Temporary Excel lock files (those whose names begin with <c>~$</c>) are automatically skipped. </remarks>
    /// <param name="importDir">Directory to search for Excel files.</param>
    /// <param name="pattern">Glob pattern used to filter files (e.g., <c>*Visit_Stats*.xlsx</c>).</param>
    /// <param name="worksheetHandler">Callback invoked for each worksheet <see cref="DataTable"/> and its sheet name.</param>
    /// <param name="statusCallback">Optional callback to report per-file progress messages.</param>
    private static void Process(string importDir, string pattern, Action<DataTable, string> worksheetHandler, Action<string>? statusCallback = null)
    {
        string[] matchingFiles = Directory.GetFiles(importDir, pattern, SearchOption.TopDirectoryOnly);
        int processedCount     = 0;
        int totalFiles         = matchingFiles.Length;

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
                    worksheetHandler(worksheet, worksheet.TableName);
                }
            }

            processedCount++;
        }
    }
}