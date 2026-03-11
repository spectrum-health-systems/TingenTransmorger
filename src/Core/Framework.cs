// 260204_code
// 260311_documentation

using System.IO;
using System.Windows;

namespace TingenTransmorger.Core;

/// <summary>Provides methods for verifying application framework requirements at startup.</summary>
class Framework
{
    /// <summary>Verifies that all directories defined in the configuration exist.</summary>
    /// <remarks>Admin directories are also verified when <paramref name="config"/> has <c>Mode</c> set to <c>Admin</c>.</remarks>
    /// <param name="config">The application configuration containing the directories to verify.</param>
    internal static void Verify(Configuration config)
    {
        VerifyDirectories(config.StandardDirectories);

        if (config.Mode == "Admin")
        {
            VerifyDirectories(config.AdminDirectories);
        }
    }

    /// <summary>Verifies that each directory in a collection exists, prompting or stopping the application as needed.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Null or empty path values display an error message and stop the application.</item>
    /// <item>Non-existent paths prompt the user to create the directory or exit.</item>
    /// </list>
    /// </remarks>
    /// <param name="directories">Dictionary of configuration key/path pairs to verify.</param>
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

    /// <summary>Prompts the user to create a missing directory, stopping the application if declined.</summary>
    /// <remarks>Calls <see cref="MainWindow.StopApp"/> if the user selects <b>No</b>.</remarks>
    /// <param name="dir">The key/path pair describing the missing directory.</param>
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

    /// <summary>Displays an error for a null or empty configuration directory value and stops the application.</summary>
    /// <remarks>Calls <see cref="MainWindow.StopApp"/> unconditionally after displaying the warning.</remarks>
    /// <param name="dir">The key/path pair whose configured path is null or empty.</param>
    private static void NullEmptyWarning(KeyValuePair<string, string> dir)
    {
        string[] msgboxContent = Catalog.msgbox_InvalidConfigurationSetting(dir.Key);

        _ = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.OK, MessageBoxImage.Error);

        MainWindow.StopApp();
    }
}