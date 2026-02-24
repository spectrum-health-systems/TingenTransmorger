// 260219_code
// 260224_documentation

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
        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
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