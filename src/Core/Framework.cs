// 260204_code
// 260204_documentation

using System.IO;
using System.Windows;

namespace TingenTransmorger.Core;

/// <summary>
/// Provides framework-level validation for configuration directory settings.
/// </summary>
/// <remarks>
/// The methods in this class validate configured directories and interact with the user via message boxes when values
/// are missing or paths do not exist. In certain cases the user will be prompted to create missing directories or the
/// application will be terminated via <see cref="MainWindow.StopApp"/>.
/// </remarks>
class Framework
{
    /// <summary>
    /// Verifies the configuration by validating standard directories and, when in admin mode, administrator-only
    /// directories.
    /// </summary>
    /// <param name="config">
    /// The configuration instance to validate.
    /// </param>
    internal static void Verify(Configuration config)
    {
        VerifyDirectories(config.StandardDirectories);

        if (config.Mode == "Admin")
        {
            VerifyDirectories(config.AdminDirectories);
        }
    }

    /// <summary>
    /// Validates a set of directory mappings. For each entry this method ensures that a non-empty path exists and that
    /// the target directory exists on disk.
    /// </summary>
    /// <param name="directories">
    /// A dictionary mapping configuration keys to directory paths.
    /// </param>
    /// <remarks>
    /// This method will display message boxes for missing or invalid values and  will prompt the user to create missing
    /// directories. Choosing not to create a required directory will terminate the application.
    /// </remarks>
    internal static void VerifyDirectories(Dictionary<string, string> directories)
    {
        foreach (KeyValuePair<string, string> dir in directories)
        {
            if (string.IsNullOrEmpty(dir.Value))
            {
                NullEmptyWarning(dir);
            }

            if (!Directory.Exists(dir.Value))
            {
                PromptToCreateDirectory(dir);
            }
        }
    }

    /// <summary>
    /// Prompts the user to create a directory when a configured path does not exist.
    /// </summary>
    /// <param name="dir">
    /// A key/value pair where the key is the configuration name and the value is the path.
    /// </param>
    /// <remarks>
    /// If the user agrees the directory will be created. If the user declines the application will be terminated
    /// via <see cref="MainWindow.StopApp"/>.
    /// </remarks>
    private static void PromptToCreateDirectory(KeyValuePair<string, string> dir)
    {
        string[] msgboxContent = Catalog.msgbox_PathDoesNotExistWithCreatePrompt(dir.Key, dir.Value);

        MessageBoxResult result = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.YesNo, MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
        {
            Directory.CreateDirectory(dir.Value);
        }
        else
        {
            MainWindow.StopApp();
        }
    }

    /// <summary>
    /// Shows an error message for a missing or empty configuration setting and terminates the application.
    /// </summary>
    /// <param name="dir">
    /// A key/value pair where the key is the configuration name and the value is the path.
    /// </param>
    private static void NullEmptyWarning(KeyValuePair<string, string> dir)
    {
        string[] msgboxContent = Catalog.msgbox_InvalidConfigurationSetting(dir.Key);

        _ = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.OK, MessageBoxImage.Error);

        MainWindow.StopApp();
    }
}