// 260206_code
// 260206_documentation

using System.IO;
using System.Text;
using System.Text.Json;

namespace TingenTransmorger.Database;

/// <summary>
/// Internal utility for reading and writing small JSON files used by the database construction process.
/// </summary>
/// <remarks>
/// <para>
/// This helper is intentionally internal and static. It provides simple, synchronous file I/O helpers:
/// - Read a JSON file and de-serialize it to a list of dictionaries.
/// - Read a JSON file and de-serialize it to a generic object.
/// - Serialize the in-memory database to both a pretty-printed JSON file and a compact database file, then copy the
/// compact file to the master database directory.
/// </para>
/// <para>
/// All methods operate with UTF-8 encoding. IO and JSON parsing exceptions (for example <see cref="IOException"/> or
/// <see cref="JsonException"/>) are not swallowed and will propagate to the caller.
/// </para>
/// </remarks>
internal static class JsonFileReader
{
    /// <summary>
    /// Default <see cref="JsonSerializerOptions"/> used when writing a human-readable JSON database file.
    /// </summary>
    /// <remarks>
    /// The option <see cref="JsonSerializerOptions.WriteIndented"/> is enabled so that the generated `transmorger.json`
    /// file is formatted for easier inspection during development and debugging.
    /// </remarks>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Reads a JSON file from the specified directory and de-serializes it to a list of dictionary objects.
    /// </summary>
    /// <param name="dir">
    /// Directory that contains the JSON file.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to read.
    /// </param>
    /// <remarks>
    /// The method reads the entire file using UTF-8 and uses <see cref="JsonSerializer.Deserialize{T}"/> to parse it.
    /// </remarks>
    /// <returns>
    /// A <see cref="List{T}"/> of dictionaries representing the JSON array content, or <c>null</c> if the file does not
    /// exist. Each dictionary maps property names to de-serialized values which may be <c>null</c>.
    /// </returns>

    public static List<Dictionary<string, object?>>? ReadJsonList(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);

        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path, Encoding.UTF8);

        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json);
    }

    /// <summary>
    /// Reads a JSON file from the specified directory and de-serializes it to a generic object.
    /// </summary>
    /// <param name="dir">
    /// Directory that contains the JSON file.
    /// </param>
    /// <param name="fileName">
    /// Name of the JSON file to read.
    /// </param>
    /// <returns>
    /// The de-serialized object, or <c>null</c> if the file does not exist.
    /// </returns>
    /// <remarks>
    /// This method is useful for reading JSON content whose structure is not known at compile time. The returned object
    /// will typically be a <see cref="System.Text.Json.JsonElement"/>, array, or dictionary  depending on the top-level
    /// JSON value.
    /// </remarks>
    public static object? ReadJsonObject(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);

        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path, Encoding.UTF8);

        return JsonSerializer.Deserialize<object>(json);
    }

    /// <summary>
    /// Serializes the provided in-memory database and writes the resulting files to disk.
    /// </summary>
    /// <param name="tmpDir">
    /// Temporary directory where output files are first written.
    /// </param>
    /// <param name="masterDbDir">
    /// Directory where the final compact database file is copied to.
    /// </param>
    /// <param name="database">
    /// The in-memory database represented as a dictionary of named sections.
    /// </param>
    /// <remarks>
    /// <para>
    /// Behavior:
    /// - Writes a pretty-printed JSON file named <c>transmorger.json</c> in <paramref name="tmpDir"/> using <see cref="_jsonOptions"/>.
    /// - Writes a compact JSON file named <c>transmorger.db</c> in <paramref name="tmpDir"/> (no indentation).
    /// - Copies the compact <c>transmorger.db</c> file to the <paramref name="masterDbDir"/>, overwriting any existing
    ///   file.
    /// </para>
    /// <para>All operations use UTF-8 encoding. IO exceptions and serialization exceptions will propagate to the caller.</para>
    /// </remarks>
    public static void WriteDatabaseFiles(string tmpDir, string masterDbDir, Dictionary<string, object?> database)
    {
        var jsonPath = Path.Combine(tmpDir, "transmorger.json");
        var json     = JsonSerializer.Serialize(database, _jsonOptions);
        File.WriteAllText(jsonPath, json, Encoding.UTF8);

        var dbTempPath = Path.Combine(tmpDir, "transmorger.db");
        var db         = JsonSerializer.Serialize(database);
        File.WriteAllText(dbTempPath, db, Encoding.UTF8);

        var masterDbPath = Path.Combine(masterDbDir, "transmorger.db");
        File.Copy(dbTempPath, masterDbPath, true);
    }
}