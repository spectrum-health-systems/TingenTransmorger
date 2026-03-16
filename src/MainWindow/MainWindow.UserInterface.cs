// 260227_code
// 260311_documentation

using System.Windows;
using System.Windows.Controls;

namespace TingenTransmorger;

/* The MainWindow.UserInterface partial class contains logic specific to the user interface.
 */
public partial class MainWindow : Window
{
    /// <summary>Initializes the UI to its default state with patient search active.</summary>
    /// <remarks>Sets the search toggle to 'Patient Search' and checks the search-by-name radio button.</remarks>
    private void SetInitialUi()
    {
        ClearUi();

        btnSearchToggle.Content    = "Patient Search";
        rbtnSearchByName.IsChecked = true;
    }

    /// <summary>Resets all UI component groups to their default visibility states.</summary>
    /// <remarks>Delegates to individual Reset methods and collapses the meeting detail and detail panels.</remarks>
    private void ResetAllComponents()
    {
        ResetUserDetailUi();
        ResetMeetingResultUi();
        ResetGeneralMeetingDetailUi();
        ResetPatientMeetingDetailUi();
        ResetProviderMeetingDetailUi();

        spnlMeetingDetail.Visibility = Visibility.Collapsed;
        spnlDetail.Visibility        = Visibility.Collapsed;
    }

    /// <summary>Resets the user detail panel visibility to its default state.</summary>
    /// <remarks>Shows the name/ID and contacts panels; collapses the user detail panel.</remarks>
    private void ResetUserDetailUi()
    {
        spnlUserNameAndId.Visibility = Visibility.Visible;
        spnlUserContacts.Visibility  = Visibility.Visible;
        spnlUserDetail.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Resets the meeting result panel visibility to its default state.</summary>
    /// <remarks>Shows the meeting breakdown and data grid; collapses the meeting result panel.</remarks>
    private void ResetMeetingResultUi()
    {
        spnlMeetingBreakdown.Visibility = Visibility.Visible;
        dgrdMeetingList.Visibility      = Visibility.Visible;
        spnlMeetingResult.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Resets the general meeting detail panel visibility to its default state.</summary>
    /// <remarks>Shows the top, left, center, and right column panels; collapses the detail border.</remarks>
    private void ResetGeneralMeetingDetailUi()
    {
        spnlGeneralMeetingDetailTop.Visibility          = Visibility.Visible;
        spnlGeneralMeetingDetailLeftColumn.Visibility   = Visibility.Visible;
        spnlGeneralMeetingDetailCenterColumn.Visibility = Visibility.Visible;
        spnlGeneralMeetingDetailRightColumn.Visibility  = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility             = Visibility.Collapsed;
    }

    /// <summary>Resets the patient meeting detail panel visibility to its default state.</summary>
    /// <remarks>Shows the top, left, center, and right column panels; collapses the detail border.</remarks>
    private void ResetPatientMeetingDetailUi()
    {
        spnlPatientMeetingDetailTop.Visibility          = Visibility.Visible;
        spnlPatientMeetingDetailLeftColumn.Visibility   = Visibility.Visible;
        spnlPatientMeetingDetailCenterColumn.Visibility = Visibility.Visible;
        spnlPatientMeetingDetailRightColumn.Visibility  = Visibility.Visible;
        brdrPatientMeetingDetail.Visibility             = Visibility.Collapsed;
    }

    /// <summary>Resets the provider meeting detail panel visibility to its default state.</summary>
    /// <remarks>Shows the top and participant names panels; collapses the detail border.</remarks>
    private void ResetProviderMeetingDetailUi()
    {
        spnlProviderMeetingDetailTop.Visibility = Visibility.Visible;
        spnlProviderParticipantNames.Visibility = Visibility.Visible;
        brdrProviderMeetingDetail.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Applies the admin mode visual theme to the main window.</summary>
    /// <remarks>Sets the window border to purple and appends '- ADMIN MODE' to the window title.</remarks>
    private void SetAdminModeTheme()
    {
        brdrMainWindow.Background = System.Windows.Media.Brushes.Purple;
        var currentTitle = Title;
        Title = $"{currentTitle} - ADMIN MODE";
    }

    /// <summary>Applies the out-of-date database visual theme to the main window.</summary>
    /// <remarks>Sets the window border to red and appends '- DATABASE IS OUT OF DATE' to the window title.</remarks>
    public void SetOutOfDateDatabaseTheme()
    {
        brdrMainWindow.Background = System.Windows.Media.Brushes.Red;
        var currentTitle = Title;
        Title = $"{currentTitle} - DATABASE IS OUT OF DATE";
    }

    /// <summary>Configures the UI layout for displaying patient details.</summary>
    /// <remarks>Calls <see cref="ResetAllComponents"/> before making patient-specific panels visible.</remarks>
    /// <param name="patientName">The full name of the patient to display.</param>
    /// <param name="patientId">The unique identifier of the patient to display.</param>
    private void SetPatientDetailUi(string patientName, string patientId)
    {
        ResetAllComponents();

        SetUserDetail("PATIENT", patientName, patientId);

        spnlUserDetail.Visibility            = Visibility.Visible;
        spnlMeetingResult.Visibility         = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
        brdrPatientMeetingDetail.Visibility  = Visibility.Visible;

        spnlMeetingDetail.Visibility         = Visibility.Visible;
        spnlDetail.Visibility                = Visibility.Visible;
    }

    /// <summary>Configures the UI layout for displaying provider details.</summary>
    /// <remarks>Hides the contacts and patient meeting detail panels, which are not used for provider display.</remarks>
    /// <param name="providerName">The full name of the provider to display.</param>
    /// <param name="providerId">The unique identifier of the provider to display.</param>
    private void SetProviderDetailUi(string providerName, string providerId)
    {
        SetUserDetail("PROVIDER", providerName, providerId);

        spnlUserDetail.Visibility            = Visibility.Visible;
        spnlMeetingResult.Visibility         = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
        brdrProviderMeetingDetail.Visibility = Visibility.Visible;

        /* These panels are not currently used with provider details, so they are hidden.
         */
        spnlUserContacts.Visibility         = Visibility.Collapsed;
        brdrPatientMeetingDetail.Visibility = Visibility.Collapsed;

        spnlMeetingDetail.Visibility        = Visibility.Visible;
        spnlDetail.Visibility               = Visibility.Visible;
    }

    /// <summary>Populates the user type, name, and ID labels in the detail panel.</summary>
    /// <remarks>Used by both <see cref="SetPatientDetailUi"/> and <see cref="SetProviderDetailUi"/>.</remarks>
    /// <param name="searchType">The user type label to display (e.g., 'PATIENT' or 'PROVIDER').</param>
    /// <param name="userName">The name of the user to display.</param>
    /// <param name="userId">The unique identifier of the user to display.</param>
    private void SetUserDetail(string searchType, string userName, string userId)
    {
        lblUserTypeKey.Content   = searchType;
        lblUserNameValue.Content = userName;
        lblUserIdValue.Content   = userId;
    }

    /// <summary>Toggles the search type button between patient and provider search, then clears the UI.</summary>
    /// <remarks>Calls <see cref="ClearUi"/> after toggling to reset the search state.</remarks>
    private void SetSearchToggleUi()
    {
        btnSearchToggle.Content = btnSearchToggle.Content.ToString() == "Patient Search"
            ? "Provider Search"
            : "Patient Search";

        ClearUi();
    }

    /// <summary>Clears the search box and results list, then resets all UI components.</summary>
    /// <remarks>Calls <see cref="ResetAllComponents"/> as part of the clear operation.</remarks>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();

        ResetAllComponents();
    }

    /// <summary>Sets the color and enabled state of a message detail button based on failure and delivery data.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>Yellow</b> — both failures and deliveries exist.</item>
    /// <item><b>Green</b> — deliveries exist with no failures.</item>
    /// <item><b>Red</b> — failures exist with no deliveries.</item>
    /// <item><b>Gray</b> — no records exist; the button is also disabled.</item>
    /// </list>
    /// </remarks>
    /// <param name="hasFailures">Whether the patient has message failure records for this contact.</param>
    /// <param name="hasDeliveries">Whether the patient has message delivery records for this contact.</param>
    /// <param name="theButton">The button whose color and enabled state will be updated.</param>
    private static void UpdateDetailsButtonColor(bool hasFailures, bool hasDeliveries, Button theButton)
    {
        theButton.IsEnabled = true;

        if (hasFailures && hasDeliveries)
        {
            theButton.Background = System.Windows.Media.Brushes.Yellow;
        }
        else if (hasDeliveries)
        {
            theButton.Background = System.Windows.Media.Brushes.Green;
        }
        else if (hasFailures)
        {
            theButton.Background = System.Windows.Media.Brushes.Red;
        }
        else
        {
            // No records: gray background, disabled
            theButton.Background = System.Windows.Media.Brushes.Gray;
            theButton.IsEnabled = false;
        }
    }
}