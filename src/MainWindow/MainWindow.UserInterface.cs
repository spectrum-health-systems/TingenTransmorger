// 260225_code
// 260225_documentation

using System.Windows;
using System.Windows.Controls;

namespace TingenTransmorger;

/* The MainWindow.UserInterface partial class contains logic specific to the user interface.
 */
public partial class MainWindow : Window
{
    /// <summary>Setup the initial user interface.</summary>
    private void SetInitialUi()
    {
        rbtnSearchByName.IsChecked                       = true;
        spnlDetail.Visibility  = Visibility.Collapsed;
        spnlMeeting.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetail.Visibility          = Visibility.Collapsed;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="patientName">The name of the patient.</param>
    /// <param name="patientId">The ID of the patient.</param>
    private void SetPatientDetailUi(string patientName, string patientId)
    {
        lblUserTypeKey.Content               = "PATIENT";
        lblUserNameValue.Content             = patientName;
        lblUserIdValue.Content               = patientId;
        spnlDetail.Visibility                = Visibility.Visible;
        spnlMeeting.Visibility               = Visibility.Visible;
        spnlMeetingDetail.Visibility         = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
        brdrPatientMeetingDetail.Visibility  = Visibility.Visible;
        spnlUserContacts.Visibility          = Visibility.Visible;
        brdrProviderMeetingDetail.Visibility = Visibility.Collapsed;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="providerId">The ID of the provider.</param>
    private void SetProviderDetailUi(string providerName, string providerId)
    {
        lblUserTypeKey.Content              = "PROVIDER";
        lblUserNameValue.Content            = providerName;
        lblUserIdValue.Content              = providerId;
        spnlDetail.Visibility               = Visibility.Visible;
        spnlMeeting.Visibility              = Visibility.Visible;
        spnlMeetingDetail.Visibility        = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility = Visibility.Collapsed;
        brdrPatientMeetingDetail.Visibility = Visibility.Collapsed;
        spnlUserContacts.Visibility         = Visibility.Collapsed;

    }

    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();

        spnlDetail.Visibility        = Visibility.Collapsed;
        spnlMeeting.Visibility       = Visibility.Collapsed;
        spnlMeetingDetail.Visibility = Visibility.Collapsed;
    }

    /// <summary>Toggle the search type button text.</summary>
    private void SetSearchToggleUi()
    {
        btnSearchToggle.Content = btnSearchToggle.Content.ToString() == "Patient Search"
            ? "Provider Search"
            : "Patient Search";

        ClearUi();
    }

    /// <summary>Updates the btnPhoneDetails and btnEmailDetails button appearance based on SMS failure and delivery records.</summary>
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