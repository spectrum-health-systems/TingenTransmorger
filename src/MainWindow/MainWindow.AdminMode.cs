// 260227_code
// 260227_documentation

using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/* The MainWindow.AdminMode partial class contains logic related to admin mode.
 */
public partial class MainWindow : Window
{
    /// <summary>Handle admin mode operations.</summary>
    /// <remarks>
    /// Currently admin mode is focused on rebuilding the Transmorger database, but I'm leaving this the way it is for
    /// now in case we want to add more admin-related operations in the future.
    /// </remarks>
    /// <param name="importDir">The directory for importing data.</param>
    /// <param name="tmpDir">The temporary directory for various operations.</param>
    /// <param name="masterDbDir">The directory of the master database.</param>
    /// <returns>Asynchronous task.</returns>
    private async Task<bool> EnterAdminMode(string importDir, string tmpDir, string masterDbDir)
    {
        if (!RebuildDatabasePrompt())
        {
            StopApp();
        }

        return await RebuildDatabase(importDir, tmpDir, masterDbDir);
    }

    /// <summary> Rebuild the Transmorger database.</summary>
    /// <param name="importDir">The directory for importing data.</param>
    /// <param name="tmpDir">The temporary directory for various operations.</param>
    /// <param name="masterDbDir">The directory of the master database.</param>
    /// <returns>The Transmorger database.</returns>
    private async Task<bool> RebuildDatabase(string importDir, string tmpDir, string masterDbDir)
    {
        SetAdminModeTheme();
        Hide();

        return await TransmorgerDatabase.Rebuild(importDir, tmpDir, masterDbDir, this);
    }

    /// <summary>Prompts the user to confirm if they want to rebuild the database.</summary>
    /// <returns>True if the user confirms, otherwise false.</returns>
    private static bool RebuildDatabasePrompt()
    {
        var msgboxContent                = Catalog.msgbox_DatabaseRebuildCheck();
        MessageBoxResult rebuildResponse = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.YesNo, MessageBoxImage.Error);

        return rebuildResponse == MessageBoxResult.Yes;
    }

    /// <summary>Set the UI theme for admin mode.</summary>
    private void SetAdminModeTheme()
    {
        brdrMainWindow.Background = System.Windows.Media.Brushes.Red;
    }
}