// 260311_code
// 260311_documentation

using System.IO;
using System.Text;

namespace TingenTransmorger.TeleHealthReport;

/// <summary>Provides methods for processing TeleHealth report workbooks.</summary>
static class ReportProcessor
{
    /// <summary>Processes Visit Statistics reports with progress callback support.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessVisitStats(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.VisitStats(importDir, tmpDir, statusCallback);
    }

    /// <summary>Processes Visit Details reports with progress callback support.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessVisitDetails(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.VisitDetails(importDir, tmpDir, statusCallback);
    }

    /// <summary>Processes Visit Details reports with progress callback support.</summary>
    /// <param name="importDir">Directory containing source Excel files.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="statusCallback">Optional callback to report status messages.</param>
    internal static void ProcessMessageFailure(string importDir, string tmpDir, Action<string>? statusCallback = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Directory.CreateDirectory(tmpDir);
        ProcessWorkbook.MessageFailure(importDir, tmpDir, statusCallback);
    }

    /// <summary>Processes Message Delivery reports with progress callback support.</summary>
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