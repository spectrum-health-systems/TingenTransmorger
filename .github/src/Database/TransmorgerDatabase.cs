// 260206_code
// 260206_documentation

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TingenTransmorger.Database;

/// <summary>Builds the Transmorger database from processed report JSON files.</summary>
public class TransmorgerDatabase
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserves special characters like ' and -
    };

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    // Holds the parsed database JSON when loaded via Load()
    private JsonElement _jsonRoot;
    private bool _hasData;

    internal static TransmorgerDatabase Load(string localDb)
    {
        // If no path supplied, default to "Database/transmorger.db" under app base
        if (string.IsNullOrWhiteSpace(localDb))
        {
            localDb = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), "Database", "transmorger.db");
        }

        // Resolve path: try as provided, then relative to application base directory
        var path = localDb;
        if (!File.Exists(path))
        {
            var alt = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), localDb);
            if (File.Exists(alt))
            {
                path = alt;
            }
            else
            {
                throw new FileNotFoundException($"Database file not found: {localDb}", localDb);
            }
        }

        var json = File.ReadAllText(path, Encoding.UTF8);

        using var doc = JsonDocument.Parse(json);
        var instance = new TransmorgerDatabase();


        instance._jsonRoot = doc.RootElement.Clone();
        instance._hasData = true;

        return instance;
    }

    /// <summary>Returns the VisitStats section from the loaded database as pretty JSON.</summary>
    public string GetSummaryVisitStatsJson()
    {
        if (!_hasData)
            return string.Empty;
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
            return string.Empty;
        if (!summary.TryGetProperty("VisitStats", out var visit))
            return string.Empty; // Check for VisitStats
        return JsonSerializer.Serialize(visit, JsonOptions);
    }

    /// <summary>Returns the MessageFailure section from the loaded database as pretty JSON.</summary>
    public string GetSummaryMessageFailureJson()
    {
        if (!_hasData)
            return string.Empty;
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
            return string.Empty;
        if (!summary.TryGetProperty("MessageFailure", out var mf))
            return string.Empty; // Check for MessageFailure
        return JsonSerializer.Serialize(mf, JsonOptions);
    }
    /// <summary>Entry point to build the transmorger database. Delegates to DatabaseBuilder.</summary>
    internal static void Build(string tmpDir, string masterDbDir)
    {
        DatabaseBuilder.Build(tmpDir, masterDbDir);
    }

}
