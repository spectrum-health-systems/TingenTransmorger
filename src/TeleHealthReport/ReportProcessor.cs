// 260205_code
// 260205_documentation

using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Processes TeleHealth Excel reports and converts them to JSON format.</summary>
/// <remarks>
///     This processor handles four types of reports:
///     <list type="bullet">
///         <item>Visit Stats - Summary and Meeting Errors</item>
///         <item>Visit Details - Meeting Details and Participant Details</item>
///         <item>Message Failure - Summary, SMS Stats, and Email Stats</item>
///         <item>Message Delivery - Message Delivery Stats</item>
///     </list>
/// </remarks>
static class ReportProcessor
{
    private static readonly ExcelDataSetConfiguration ExcelConfig = new()
    {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
    };

    /// <summary>Processes all TeleHealth reports from the import directory and generates JSON output files.</summary>
    /// <param name="config">Configuration object containing import and output directory paths.</param>
    internal static void Process(string importDir, string tmpDir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);

        VisitStatsReport.ProcessVisitStatsReports(importDir, tmpDir);
        VisitDetailReport.ProcessVisitDetailsReports(importDir, tmpDir);
        MessageFailureReport.ProcessMessageFailureReports(importDir, tmpDir);
        MessageDeliveryReport.ProcessMessageDeliveryReports(importDir, tmpDir);
    }

    /// <summary>Processes Excel files matching a pattern and invokes a callback for each worksheet.</summary>
    /// <param name="importDir">Directory to search for Excel files.</param>
    /// <param name="pattern">File search pattern (e.g., "*Visit_Stats*.xlsx").</param>
    /// <param name="processSheet">Callback action that receives each DataTable and sheet name.</param>
    internal static void ProcessExcelFiles(string importDir, string pattern, Action<DataTable, string> processSheet)
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