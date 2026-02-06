using System.Text.Json;

namespace TingenTransmorger.Database;

internal static class SummaryBuilder
{
    public static Dictionary<string, object?> Build(string tmpDir)
    {
        var visit = JsonFileReader.ReadJsonObject(tmpDir, "Visit_Stats-Summary.json");
        var mf = JsonFileReader.ReadJsonObject(tmpDir, "Message_Failure-Summary.json");

        return new Dictionary<string, object?>
        {
            ["VisitStats"] = visit,
            ["MessageFailure"] = mf
        };
    }
}
