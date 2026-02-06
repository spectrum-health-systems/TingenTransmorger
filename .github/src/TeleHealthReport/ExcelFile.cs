// 260206_code
// 260206_documentation

/* I used Claude Sonnet 4.5 to help with working through Excel files, so some of this stuff is a little outside my
 * wheelhouse (e.g., ExcelDataReader, DataSet, DataTable, etc.). I've researched and commented things as best I can.
 */

using System.Data;
using System.IO;
using ExcelDataReader;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>
/// Processes Excel files.
/// </summary>
/// <remarks>
/// TeleHealth reports are provided as Excel files, and this class contains logic to read those files and extract data
/// from them. It uses the ExcelDataReader library to facilitate reading Excel files into .NET data structures.
/// </remarks>
static class ExcelFile
{
    /// <summary>
    /// Configuration used by the ExcelDataReader to interpret the structure of Excel files.
    /// </summary>
    /// <remarks>
    /// This is used to specify how the ExcelDataReader should interpret the structure of the Excel files, and sets the
    /// reader to use the first row of each worksheet as column headers when reading Excel files.
    /// <remarks>
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    /// <summary>
    /// Processes TeleHealth Message Delivery reports.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Directory where JSON output files will be written.
    /// </param>
    internal static void MessageDelivery(string importDir, string tmpDir)
    {
        var allRecords = new List<Dictionary<string, object?>>();
        var headers    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process(importDir, "*Message_Delivery*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Message Delivery Stats", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.FlatSheet(worksheet, allRecords, headers);
            }
        });

        ReportUtility.WriteFlatJson(tmpDir, "Message_Delivery-Message_Delivery_Stats.json", allRecords);
    }

    /// <summary>
    /// Processes Message Failure reports containing delivery summaries and client statistics.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Directory where JSON output files will be written.
    /// </param>
    internal static void MessageFailure(string importDir, string tmpDir)
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
                ReportWorksheet.SummarySheet(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("SMS STATS", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.ClientStatsSheet(worksheet, smsStatsByClient, smsStatsHeaders);
            }
            else if (sheetName.Equals("EMAIL STATS", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.ClientStatsSheet(worksheet, emailStatsByClient, emailStatsHeaders);
            }
        });

        ReportUtility.WriteSummaryJson(tmpDir, "Message_Failure-Summary.json", summaryMetrics, summaryHeaders);
        ReportUtility.WriteClientStatsJson(tmpDir, "Message_Failure-Sms_Stats.json", smsStatsByClient);
        ReportUtility.WriteClientStatsJson(tmpDir, "Message_Failure-Email_Stats.json", emailStatsByClient);
    }

    /// <summary>
    /// Processes Visit Details reports containing meeting and participant information.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Directory where JSON output files will be written.
    /// </param>
    internal static void VisitDetails(string importDir, string tmpDir)
    {
        var meetingDetailsById    = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var participantDetails        = new List<Dictionary<string, object?>>();
        var participantDetailsHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Process(importDir, "*Visit_Details*.xlsx", (worksheet, sheetName) =>
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

    /// <summary>
    /// Processes Visit Stats reports containing summary metrics and meeting errors.
    /// </summary>
    /// <param name="importDir">
    /// Directory containing source Excel files.
    /// </param>
    /// <param name="tmpDir">
    /// Directory where JSON output files will be written.
    /// </param>
    internal static void VisitStats(string importDir, string tmpDir)
    {
        var summaryMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        (string, string)? summaryHeaders = null;

        var meetingErrorsById   = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var meetingErrorHeaders = new List<string>();

        Process(importDir, "*Visit_Stats*.xlsx", (worksheet, sheetName) =>
        {
            if (sheetName.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.SummarySheet(worksheet, summaryMetrics, ref summaryHeaders);
            }
            else if (sheetName.Equals("Meeting Errors", StringComparison.OrdinalIgnoreCase))
            {
                ReportWorksheet.KeyedSheet(worksheet, meetingErrorsById, meetingErrorHeaders, "Meeting ID", aggregateNumeric: true);
            }
        });

        ReportUtility.WriteSummaryJson(tmpDir, "Visit_Stats-Summary.json", summaryMetrics, summaryHeaders);
        ReportUtility.WriteKeyedJson(tmpDir, "Visit_Stats-Meeting_Errors.json", meetingErrorsById);
    }

    /// <summary>
    /// Processes Excel files matching a pattern and invokes a callback for each worksheet.
    /// </summary>
    /// <remarks>
    /// When searching for files in the import directory, we'll use SearchOption.TopDirectoryOnly to ensure that only
    /// the specified directory is searched, and not any subdirectories.<br/>
    /// <br/>
    /// Iterate each matching file path. Each item is the absolute path to a file that matched the pattern. The loop
    /// opens each file for read access and processes all worksheets within it.
    /// </remarks>
    /// <param name="importDir">
    /// Directory to search for Excel files.
    /// </param>
    /// <param name="pattern">
    /// File search pattern (e.g., "*Visit_Stats*.xlsx").
    /// </param>
    /// <param name="processSheet">
    /// Callback action that receives each DataTable and sheet name.
    /// </param>
    private static void Process(string importDir, string pattern, Action<DataTable, string> processSheet)
    {
        string[] matchingFiles = Directory.GetFiles(importDir, pattern, SearchOption.TopDirectoryOnly);

        foreach (string filePath in matchingFiles)
        {
            using FileStream fileStream        = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(fileStream);

            DataSet dataSet = excelReader.AsDataSet(ExcelConfig); //

            foreach (DataTable worksheet in dataSet.Tables)
            {
                if (worksheet.Columns.Count > 0)
                {
                    processSheet(worksheet, worksheet.TableName);
                }
            }
        }
    }
}