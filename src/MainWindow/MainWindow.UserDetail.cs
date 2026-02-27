// 260227_code
// 260227_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.UserDetails partial class contains logic related to displaying user (either a patient or a provider)
 * details.
 *
 * This class does not handle any meeting-specific details, those are handled in MainWindow.MeetingDetails.
 */
public partial class MainWindow : Window
{
    /// <summary>Display details for the selected user.</summary>
    /// <remarks>
    /// A "user" in this context refers to either a patient or a provider.<br/>
    /// <br/>
    /// When a user is selected from the search results, this method will:
    /// <list type="number">
    /// <item>Change the UserTypeKey contents to "PATIENTS" or "PROVIDERS" based on the search type</item>
    /// <item>Display the user name</item>
    /// <item>Display the user ID</item>
    /// <item>Display the patient phone number(s), and set the detail button</item>
    /// <item>Display the patient email addresses(s), and set the detail button</item>
    /// <item>Display the user's meting breakdown</item>
    /// <item>Display the user's meting list</item>
    /// </list>
    /// </remarks>
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

    /// <summary>Extract the user name and ID.</summary>
    /// <param name="selectedItem">The string containing the name and ID.</param>
    /// <returns>A string array where [0] = name and [1] = ID.</returns>
    private static string[] ExtractNameId(string selectedItem)
    {
        int lastParenthesisIndex = selectedItem.LastIndexOf('(');

        return [selectedItem.Substring(0, lastParenthesisIndex).Trim(),
                selectedItem.Substring(lastParenthesisIndex + 1).TrimEnd(')').Trim()];
    }
}