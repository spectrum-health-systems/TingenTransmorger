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
        spnlDetails.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Collapsed;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="patientName">The name of the patient.</param>
    /// <param name="patientId">The ID of the patient.</param>
    private void SetPatientDetailUi(string patientName, string patientId)
    {
        lblPatientProviderKey.Content                   = "PATIENT";
        lblPatientProviderNameValue.Content             = patientName;
        lblPatientProviderIdValue.Content               = patientId;
        spnlDetails.Visibility  = Visibility.Visible;
        spnlMeetingComponents.Visibility                = Visibility.Visible;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Visible;
        brdrMeetingDetailsGeneralContainer.Visibility   = Visibility.Visible;
        brdrMeetingDetailsPatientContainer.Visibility   = Visibility.Visible;
        spnlPatientPhoneAndEmailComponents.Visibility   = Visibility.Visible;
        brdrMeetingDetailsProviderContainer.Visibility  = Visibility.Collapsed;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="providerId">The ID of the provider.</param>
    private void SetProviderDetailUi(string providerName, string providerId)
    {
        lblPatientProviderKey.Content                   = "PROVIDER";
        lblPatientProviderNameValue.Content             = providerName;
        lblPatientProviderIdValue.Content               = providerId;
        spnlDetails.Visibility  = Visibility.Visible;
        spnlMeetingComponents.Visibility                 = Visibility.Visible;
        brdrMeetingDetailsProviderContainer.Visibility  = Visibility.Visible;
        brdrMeetingDetailsGeneralContainer.Visibility   = Visibility.Collapsed;
        brdrMeetingDetailsPatientContainer.Visibility   = Visibility.Collapsed;
        spnlPatientPhoneAndEmailComponents.Visibility   = Visibility.Collapsed;

    }


    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();
        spnlDetails.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Collapsed;
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