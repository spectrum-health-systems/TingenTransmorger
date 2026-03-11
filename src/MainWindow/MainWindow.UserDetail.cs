// 260227_code
// 260311_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.UserDetails partial class contains logic related to displaying user (either a patient or a provider)
 * details.
 *
 * This class does not handle any meeting-specific details, those are handled in MainWindow.MeetingDetails.
 */
public partial class MainWindow : Window
{
    /// <summary>Loads and displays details for the currently selected patient or provider search result.</summary>
    /// <remarks>Returns early if the results list is empty, otherwise routes to patient or provider display.</remarks>
    private void Display()
    {
        var selectedItem = lstbxSearchResults.SelectedItem as string;

        /* Don't try and get details when there aren't any search results.
        */
        if (lstbxSearchResults.Items.Count == 0)
        {
            return;
        }

        var nameId = ExtractNameId(selectedItem);

        if (btnSearchToggle.Content.ToString().Contains("patient", StringComparison.OrdinalIgnoreCase))
        {
            DisplayPatientDetails(nameId[0], nameId[1]);
        }
        else if (btnSearchToggle.Content.ToString().Contains("provider", StringComparison.OrdinalIgnoreCase))
        {
            DisplayProviderDetails(nameId[0], nameId[1]);
        }
    }

    /// <summary>Parses a formatted search result string into a name and ID pair.</summary>
    /// <remarks>Expects the format <c>Name (ID)</c>; splits on the last opening parenthesis.</remarks>
    /// <param name="selectedItem">The formatted search result string to parse.</param>
    /// <returns>A two-element array where index <c>0</c> is the name and index <c>1</c> is the ID.</returns>
    private static string[] ExtractNameId(string selectedItem)
    {
        int lastParenthesisIndex = selectedItem.LastIndexOf('(');

        return [selectedItem.Substring(0, lastParenthesisIndex).Trim(),
                selectedItem.Substring(lastParenthesisIndex + 1).TrimEnd(')').Trim()];
    }
}