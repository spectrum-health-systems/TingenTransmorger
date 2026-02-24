// 260224_code
// 260224_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.Details partial class contains logic related to displaying details in the UI that are not specific to
 * patients, providers, or meetings.
 */
public partial class MainWindow : Window
{
    /// <summary>Display details for the selected item in the search results.</summary>
    private void DisplayDetails()
    {
        var selectedItem = lstbxSearchResults.SelectedItem as string;

        /* Don't try and get details when there aren't any search results.
        */
        if (lstbxSearchResults.Items.Count == 0)
        {
            return;
        }

        var lastParenthesisIndex = selectedItem.LastIndexOf('(');
        var name                 = selectedItem.Substring(0, lastParenthesisIndex).Trim();
        var id                   = selectedItem.Substring(lastParenthesisIndex + 1).TrimEnd(')').Trim();

        if (btnSearchToggle.Content.ToString().Contains("patient", StringComparison.OrdinalIgnoreCase))
        {
            DisplayPatientDetails(name, id);
        }
        else if (btnSearchToggle.Content.ToString().Contains("provider", StringComparison.OrdinalIgnoreCase))
        {
            DisplayProviderDetails(name, id);
        }
    }
}