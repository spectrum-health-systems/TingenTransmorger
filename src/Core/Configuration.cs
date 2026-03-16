// 260204_code
// 260311_documentation

using System.IO;
using System.Text.Json;

namespace TingenTransmorger.Core;

/// <summary>Represents the application configuration, persisted as a JSON file on disk.</summary>
/// <remarks>
/// The configuration file is located at <c>AppData/Config/transmorger.config</c> relative to the application working
/// directory. A default configuration is created automatically if it does not exist.
/// </remarks>
class Configuration
{
    /// <summary>Gets or sets the application operating mode.</summary>
    /// <value>A string identifying the current mode (e.g., <c>Standard</c>).</value>
    public string Mode { get; set; }

    /// <summary>Gets or sets the directory paths used in standard operating mode.</summary>
    /// <value>
    /// A dictionary mapping directory keys (e.g., <c>LocalDb</c>, <c>MasterDb</c>) to their corresponding file system
    /// paths.
    /// </value>
    public Dictionary<string, string> StandardDirectories { get; set; }

    /// <summary>Gets or sets the directory paths used in administrative operations.</summary>
    /// <value>
    /// A dictionary mapping directory keys (e.g., <c>Tmp</c>, <c>Import</c>) to their corresponding file system paths.
    /// </value>
    public Dictionary<string, string> AdminDirectories { get; set; }

    /// <summary>Serializes a <see cref="Configuration"/> instance and writes it to the configuration file.</summary>
    /// <param name="config">The <see cref="Configuration"/> instance to serialize and persist.</param>
    public static void WriteConfigFile(Configuration config)
    {
        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Config", "transmorger.config");

        string configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(configFilePath, configJson);
    }

    /// <summary>Loads the application configuration from disk, creating a default file if one does not exist.</summary>
    /// <returns>
    /// The deserialized <see cref="Configuration"/> instance read from <c>AppData/Config/transmorger.config</c>.
    /// </returns>
    internal static Configuration Load()
    {
        var appDataDirName = Path.Combine(Directory.GetCurrentDirectory(), "AppData");
        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), appDataDirName, "Config", "transmorger.config");

        if (!File.Exists(configFilePath))
        {
            Configuration config = CreateDefault(appDataDirName);
            Directory.CreateDirectory(Path.Combine(appDataDirName, "Config"));
            WriteConfigFile(config);
        }

        return JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFilePath))!;
    }

    /// <summary>Creates a default <see cref="Configuration"/> instance with preset directory paths.</summary>
    /// <param name="appDataDirName">The base application data directory path used to resolve default directory values.</param>
    /// <returns>A new <see cref="Configuration"/> instance populated with default values.</returns>
    private static Configuration CreateDefault(string appDataDirName) => new Configuration
    {
        Mode                            = "Standard",
        StandardDirectories             = new Dictionary<string, string>
        {
            { "LocalDb", "AppData/Database" },
            { "MasterDb", "" }
        },
        AdminDirectories                = new Dictionary<string, string>
        {
            { "Tmp", "AppData/Tmp" },
            { "Import", "" },
        }
    };
}