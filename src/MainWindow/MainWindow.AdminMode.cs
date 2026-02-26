// 260226_code
// 260226_documentation

using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/* The MainWindow.AdminMode partial class contains logic related to the admin mode of the application, which is
 * currently focused on rebuilding the Transmorger database.
 */
public partial class MainWindow : Window
{
    /// <summary>Handles admin mode operations.</summary>
    /// <remarks>
    ///     Currently admin mode is focused on rebuilding the Transmorger database, but I'm leaving this the way it is
    ///     for now in case we want to add more admin-related operations in the future.
    /// </remarks>
    /// <param name="importDir">The directory for importing data.</param>
    /// <param name="tmpDir">The temporary directory for various operations.</param>
    /// <param name="masterDbDir">The directory of the master database.</param>
    /// <returns>Asynchronous task.</returns>
    private async Task<bool> EnterAdminMode(string importDir, string tmpDir, string masterDbDir)
    {
        SetAdminModeTheme();
        Hide();

        if (!RebuildDatabaseYes())
        {
            StopApp();
        }

        return await TransmorgerDatabase.Rebuild(importDir, tmpDir, masterDbDir, this);
    }

    /// <summary>Prompts the user to confirm if they want to rebuild the database.</summary>
    /// <returns>True if the user confirms, otherwise false.</returns>
    private static bool RebuildDatabaseYes()
    {
        var msgboxContent                = Catalog.msgbox_DatabaseRebuildCheck();
        MessageBoxResult rebuildResponse = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.YesNo, MessageBoxImage.Error);

        return rebuildResponse == MessageBoxResult.Yes;
    }

    /// <summary>Set the theme for admin mode.</summary>
    private void SetAdminModeTheme()
    {
        this.Background = System.Windows.Media.Brushes.Red;
    }
}