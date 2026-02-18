// 260212_code
// 260212_documentation

using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

/* I've moved the MainWindow partial classes to MainWindow/ to keep the code organized, but I'm leaving the namespace as
 * TingenTransmorger instead of TingenTransmorger.MainWindow to avoid confusion with the MainWindow class.
 */
namespace TingenTransmorger;

/* Partial class MainWindow.AdminMode.cs.
 */
public partial class MainWindow : Window
{
    /// <summary>Handles admin mode operations. </summary>
    /// <remarks>Currently admin mode is focused on rebuilding the Transmorger database.</remarks>
    /// <param name="config">The Transmorger configuration object.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the database
    /// rebuild was initiated; otherwise, <see langword="false"/> if the user declined to proceed.</returns>
    private async Task<bool> EnterAdminMode(string importDir, string tmpDirImport, string masterDbDir)
    {
        SetAdminModeTheme();
        Hide();

        var msgboxContent                = Catalog.msgbox_DatabaseRebuildCheck();
        MessageBoxResult rebuildResponse = MessageBox.Show(msgboxContent[1], msgboxContent[0], MessageBoxButton.YesNo, MessageBoxImage.Error);

        if (rebuildResponse == MessageBoxResult.No)
        {
            MainWindow.StopApp();
        }

        return await TransmorgerDatabase.Rebuild(importDir, tmpDirImport, masterDbDir, this);
    }

    /// <summary>Sets the theme for admin mode.</summary>
    private void SetAdminModeTheme()
    {
        this.Background = System.Windows.Media.Brushes.Red;
    }
}