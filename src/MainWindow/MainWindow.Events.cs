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
    /// <summary>The search toggle button was clicked.</summary>
    /// <remarks>The search toggle button cycles between the Patient and Provider search modes.</remarks>
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


    /// <summary>The text in the search box was changed.</summary>
    /// <remarks>
    ///     This method is called when the user types in the search text box. It filters and displays results based on
    ///     the current search mode and search type (by name or ID).
    /// </remarks>
    private void SearchTextChanged()
    {
        lstbxSearchResults.Items.Clear();

        var searchText = txbxSearch.Text?.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        /* TODO: Probably don't need this, the database should be verified by now.
         */
        //// Don't search if database is not yet initialized
        //if (tmDb == null)
        //{
        //    return;
        //}

        switch (btnSearchToggle.Content.ToString())
        {
            case "Patient Search":
                PatientSearch(searchText);
                break;

            case "Provider Search":
                SearchProviders(searchText);
                break;
        }
    }





    /* EVENT HANDLERS */
    private void btnSearchToggle_Click(object? sender, RoutedEventArgs e) => SearchToggleClicked();
    private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => SearchTextChanged();
    private void rbtnSearch_Checked(object sender, RoutedEventArgs e) => SearchTextChanged();
    private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => SearchResultSelected();
    private void dgPatientMeetings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
    private void btnPhoneDetails_Click(object sender, RoutedEventArgs e) => PhoneDetailsClicked();
    private void btnEmailDetails_Click(object sender, RoutedEventArgs e) => EmailDetailsClicked();
    private void btnCopyMeetingDetailsGeneral_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsGeneralClicked();
    private void btnCopyMeetingDetailsPatient_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsPatientClicked();
    private void btnCopyMeetingDetailsProvider_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsProviderClicked();
}
