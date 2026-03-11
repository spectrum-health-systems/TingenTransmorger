// 260227_code
// 260311_documentation

using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/* The MainWindow.AdminMode partial class contains logic related to admin mode.
 */
public partial class MainWindow : Window
{
    /// <summary>Prompts for a database rebuild confirmation and stops the application if the user declines.</summary>
    /// <remarks>Calls <see cref="StopApp"/> before returning if the user declines the rebuild prompt.</remarks>
    /// <param name="importDir">Directory containing source Excel files to import.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="masterDbDir">Master database output directory.</param>
    /// <returns><see langword="true"/> if the database was rebuilt successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> EnterAdminMode(string importDir, string tmpDir, string masterDbDir)
    {
        if (!RebuildDatabasePrompt())
        {
            StopApp();
        }

        return await RebuildDatabase(importDir, tmpDir, masterDbDir);
    }

    /// <summary>Applies the admin mode theme, hides the main window, and rebuilds the database.</summary>
    /// <remarks>The main window remains hidden for the duration of the rebuild operation.</remarks>
    /// <param name="importDir">Directory containing source Excel files to import.</param>
    /// <param name="tmpDir">Temporary data directory.</param>
    /// <param name="masterDbDir">Master database output directory.</param>
    /// <returns><see langword="true"/> if the rebuild completed successfully; otherwise, <see langword="false"/>.</returns>
    private async Task<bool> RebuildDatabase(string importDir, string tmpDir, string masterDbDir)
    {
        SetAdminModeTheme();
        Hide();

        return await TransmorgerDatabase.Rebuild(importDir, tmpDir, masterDbDir, this);
    }

    /// <summary>Displays a confirmation message box asking whether to rebuild the database.</summary>
    /// <remarks>Uses message box content from <see cref="Catalog.msgbox_DatabaseRebuildCheck"/>.</remarks>
    /// <returns><see langword="true"/> if the user selects Yes; otherwise, <see langword="false"/>.</returns>
    private static bool RebuildDatabasePrompt()
    {
        var msgboxContent                = Catalog.msgbox_DatabaseRebuildCheck();
        MessageBoxResult rebuildResponse = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.YesNo, MessageBoxImage.Error);

        return rebuildResponse == MessageBoxResult.Yes;
    }
}