// 260226_code
// 260226_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.UserDetails partial class contains logic related to displaying user details in the right column, when a
 * user is selected from the search results.
 *
 * For example, when doing a patient search and selecting a patient from the search results, this class will:
 *
 * 1. Change the UserHeaderKey contents to "PATIENTS"
 * 2. Display the patient name
 * 3. Display the patient ID
 * 4. Display the patient phone (if one exists), and set the phone details button.
 * 5. Display the patient email address (if one exists), and set the email details button.
 * 6. Display the patient meeting breakdown
 * 7. Display the patient meeting list.
 *
 * The same steps are taken for providers.
 *
 * This class does not handle any of the meeting-specific details, that is handled in MainWindow.MeetingDetails.
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