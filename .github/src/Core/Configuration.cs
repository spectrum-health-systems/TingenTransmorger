// 260204_code
// 260204_documentation

using System.IO;
using System.Text.Json;

namespace TingenTransmorger.Core;

/// <summary>
/// Represents the application's configuration and provides helpers to load,
/// validate, and persist configuration settings to disk.
/// </summary>
/// <remarks>
/// Configuration values are stored in a JSON file at "AppData/Config/transmorger.config".
/// The <see cref="Verify"/> method performs interactive validation and may show
/// message boxes or terminate the application if required settings are missing.
/// </remarks>
class Configuration
{
    /// <summary>
    /// Gets or sets the configuration mode name (for example, "Default" or "Admin").
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application should check for
    /// database updates during startup.
    /// </summary>
    public bool CheckForDatabaseUpdateAtStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether null identifiers should be shown in search results.
    /// </summary>
    public bool ShowNullIdsInSearchResults { get; set; }

    /// <summary>
    /// Gets or sets a mapping of standard directory keys to their configured paths.
    /// </summary>
    public Dictionary<string, string> StandardDirectories { get; set; }

    /// <summary>
    /// Gets or sets administrator-only directory mappings used by advanced features.
    /// </summary>
    public Dictionary<string, string> AdminDirectories { get; set; }

    /// <summary>
    /// Writes the provided <see cref="Configuration"/> instance to the
    /// application configuration file located at "AppData/Config/transmorger.config".
    /// </summary>
    /// <param name="config">
    /// The configuration to write to disk.
    /// </param>
    /// <remarks>
    /// The containing directory is expected to be "AppData/Config" under the
    /// application's current working directory.
    /// </remarks>
    public static void WriteConfigFile(Configuration config)
    {
        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Config", "transmorger.config");

        string configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(configFilePath, configJson);
    }

    /// <summary>
    /// Loads the configuration from the application's configuration file. If the
    /// file does not exist, a default configuration is created and written to disk.
    /// </summary>
    /// <remarks>
    /// If the configuration file is absent this method creates the containing
    /// "AppData/Config" directory and writes a default configuration file.
    /// </remarks>
    /// <returns>
    /// The loaded <see cref="Configuration"/> instance.
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

    /// <summary>
    /// Creates a default configuration instance using the provided application
    /// data directory as the base for directory paths.
    /// </summary>
    /// <param name="appDataDirName">
    /// Base application data directory path.
    /// </param>
    /// <returns>
    /// A new <see cref="Configuration"/> populated with default values.
    /// </returns>
    private static Configuration CreateDefault(string appDataDirName) => new()
    {
        Mode                            = "Default",
        CheckForDatabaseUpdateAtStartup = true,
        ShowNullIdsInSearchResults      = false,
        StandardDirectories             = new Dictionary<string, string>
        {
            { "LocalDb", "" },
            { "MasterDb", "" }
        },
        AdminDirectories                = new Dictionary<string, string>
        {
            { "Tmp", "" },
            { "Import", "" },
        }
    };
}