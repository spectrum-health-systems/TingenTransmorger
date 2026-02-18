// 260212_code
// 260212_documentation

using System.Windows;

/* I've moved the MainWindow partial classes to MainWindow/ to keep the code organized, but I'm leaving the namespace as
 * TingenTransmorger instead of TingenTransmorger.MainWindow to avoid confusion with the MainWindow class.
 */
namespace TingenTransmorger;

/// <summary>Event and event handler logic.</summary>
/// <remarks>
///     This is a partial class that handles MainWindow events.
/// </remarks>
public partial class MainWindow : Window
{
    /*
     * btnSearchToggle
     */

    /// <summary>btnSearchToggle_clicked() => SearchToggleClicked().</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void btnSearchToggle_Click(object? sender, RoutedEventArgs e) => SearchToggleClicked();

    /// <summary>Set the search mode and clear the UI.</summary>
    private void SearchToggleClicked()
    {
        switch (btnSearchToggle.Content)
        {
            case "Patient Search":
                btnSearchToggle.Content = "Provider Search";
                break;

            case "Provider Search":
                btnSearchToggle.Content = "Patient Search";
                break;
        }

        ClearUi();
    }

    /*
     * txbxSearch
    */

    /// <summary>txbxSearch_TextChanged() => SearchTextChanged().</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => SearchTextChanged();

    /// <summary>The text in the search box was changed.</summary>
    /// <remarks>
    ///     This method is called when the user types in the search text box. It filters and displays results based on
    ///     the current search mode and search type (by name or ID).
    /// </remarks>
    private void SearchTextChanged()
    {
        if (string.IsNullOrWhiteSpace(txbxSearchBox.Text))
        {
            lstbxSearchResults.Items.Clear();

            return;
        }



        //var searchText = txbxSearchBox.Text?.Trim();

        //var searchType = btnSearchToggle.Content.ToString();

        DisplaySearchResults(btnSearchToggle.Content.ToString(), txbxSearchBox.Text?.Trim());
    }


    /* rbtnSearchBy
 */

    /// <summary>rbtnSearchByName_clicked()/rbtnSearchById_clicked() => SearchTextChanged()</summary>
    /// <remarks>
    ///     This method handles the Checked event for both rbtnSearchByName and rbtnSearchById.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void rbtnSearchBy_Checked(object sender, RoutedEventArgs e) => ClearUi();







    /// <summary>Handles the selection changed event for the search results list.</summary>
    /// <remarks>
    ///     This method is called when the user selects a patient or provider from the search results.
    ///     It retrieves the full details and displays them in the details panel.
    /// </remarks>
    private void SearchResultSelected()
    {
        if (lstbxSearchResults.SelectedItem == null)
        {
            return;
        }

        var searchMode = btnSearchToggle.Content.ToString();

        var selectedItem   = lstbxSearchResults.SelectedItem as string;
        var lastParenIndex = selectedItem.LastIndexOf('(');
        var name           = selectedItem.Substring(0, lastParenIndex).Trim();
        var id             = selectedItem.Substring(lastParenIndex + 1).TrimEnd(')').Trim();

        switch (searchMode)
        {
            case "Patient Search":
                DisplayPatientDetails(name, id);
                break;

            case "Provider Search":
                DisplayProviderDetails(name, id);
                break;
        }
    }

    /* EVENT HANDLERS */



    private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => SearchResultSelected();
    private void dgPatientMeetings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
    private void btnPhoneDetails_Click(object sender, RoutedEventArgs e) => PhoneDetailsClicked();
    private void btnEmailDetails_Click(object sender, RoutedEventArgs e) => EmailDetailsClicked();
    private void btnCopyMeetingDetailsGeneral_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsGeneralClicked();
    private void btnCopyMeetingDetailsPatient_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsPatientClicked();
    private void btnCopyMeetingDetailsProvider_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsProviderClicked();
}
