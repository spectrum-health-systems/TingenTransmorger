// 260219_code
// 260219_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.UserInterface partial class contains logic specific to the user interface, such as showing/hiding components,
 * updating button colors, and displaying search results. This is separate from the logic for displaying meeting details and copying data,
 * which are in their own partial classes, to keep the code organized and easier to navigate.
 */
public partial class MainWindow : Window
{
    /// <summary>Setup the initial user interface.</summary>
    private void SetupInitialUi()
    {
        rbtnSearchByName.IsChecked                       = true;
        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Collapsed;
    }

    private void SetupPatientDetailUi(string patientName, string patientId)
    {

        lblPatientProviderKey.Content                   = "PATIENT";
        lblPatientProviderNameValue.Content             = patientName;
        lblPatientProviderIdValue.Content               = patientId;
        spnlPatientProviderDetailsComponents.Visibility = Visibility.Visible;
        spnlPatientPhoneComponents.Visibility           = Visibility.Visible;
        spnlPatientEmailComponents.Visibility           = Visibility.Visible;
    }

    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();
        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Collapsed;
    }

    /// <summary>Toggle the search type button text.</summary>
    private void SetSearchToggleContent()
    {
        btnSearchToggle.Content = btnSearchToggle.Content.ToString()=="Patient Search"
            ? "Provider Search"
            : "Patient Search";

        ClearUi();
    }
}