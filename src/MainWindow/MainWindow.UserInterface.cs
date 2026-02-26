// 260226_code
// 260226_documentation

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
        ClearUi();

        btnSearchToggle.Content    = "Patient Search";
        rbtnSearchByName.IsChecked = true;
    }

    /// <summary>Reset all of the UI components.</summary>
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

    /// <summary>Reset the user detail UI components.</summary>
    private void ResetUserDetailUi()
    {
        spnlUserNameAndId.Visibility = Visibility.Visible;
        spnlUserContacts.Visibility  = Visibility.Visible;
        spnlUserDetail.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Reset the meeting result UI components.</summary>
    private void ResetMeetingResultUi()
    {
        spnlMeetingBreakdown.Visibility = Visibility.Visible;
        dgrdMeetingList.Visibility      = Visibility.Visible;
        spnlMeetingResult.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Reset the general meeting detail UI components.</summary>
    private void ResetGeneralMeetingDetailUi()
    {
        spnlGeneralMeetingDetailTop.Visibility          = Visibility.Visible;
        spnlGeneralMeetingDetailLeftColumn.Visibility   = Visibility.Visible;
        spnlGeneralMeetingDetailCenterColumn.Visibility = Visibility.Visible;
        spnlGeneralMeetingDetailRightColumn.Visibility  = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility             = Visibility.Collapsed;
    }

    /// <summary>Reset the patient meeting detail UI components.</summary>
    private void ResetPatientMeetingDetailUi()
    {
        spnlPatientMeetingDetailTop.Visibility          = Visibility.Visible;
        spnlPatientMeetingDetailLeftColumn.Visibility   = Visibility.Visible;
        spnlPatientMeetingDetailCenterColumn.Visibility = Visibility.Visible;
        spnlPatientMeetingDetailRightColumn.Visibility  = Visibility.Visible;
        brdrPatientMeetingDetail.Visibility             = Visibility.Collapsed;
    }

    /// <summary>Reset the provider meeting details UI components.</summary>
    private void ResetProviderMeetingDetailUi()
    {
        spnlProviderMeetingDetailTop.Visibility = Visibility.Visible;
        spnlProviderParticipantNames.Visibility = Visibility.Visible;
        brdrProviderMeetingDetail.Visibility    = Visibility.Collapsed;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="patientName">The name of the patient.</param>
    /// <param name="patientId">The ID of the patient.</param>
    private void SetPatientDetailUi(string patientName, string patientId)
    {
        ResetAllComponents();

        SetUserDetail("PATIENT", patientName, patientId);

        //lblUserTypeKey.Content   = "PATIENT";
        //lblUserNameValue.Content = patientName;
        //lblUserIdValue.Content   = patientId;

        spnlUserDetail.Visibility            = Visibility.Visible;
        spnlMeetingResult.Visibility         = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
        brdrPatientMeetingDetail.Visibility  = Visibility.Visible;

        spnlDetail.Visibility                = Visibility.Visible;
    }

    /// <summary>Setup the user interface for displaying patient details.</summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="providerId">The ID of the provider.</param>
    private void SetProviderDetailUi(string providerName, string providerId)
    {
        SetUserDetail("PROVIDER", providerName, providerId);

        //lblUserTypeKey.Content              = "PROVIDER";
        //lblUserNameValue.Content            = providerName;
        //lblUserIdValue.Content              = providerId;

        spnlUserDetail.Visibility            = Visibility.Visible;
        spnlMeetingResult.Visibility         = Visibility.Visible;
        brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
        brdrProviderMeetingDetail.Visibility = Visibility.Visible;

        spnlUserContacts.Visibility         = Visibility.Collapsed;
        brdrPatientMeetingDetail.Visibility = Visibility.Collapsed;

        spnlDetail.Visibility               = Visibility.Visible;
    }


    private void SetUserDetail(string searchType, string userName, string userId)
    {
        lblUserTypeKey.Content   = searchType;
        lblUserNameValue.Content = userName;
        lblUserIdValue.Content   = userId;
    }


    /// <summary>Toggle the search type button text.</summary>
    private void SetSearchToggleUi()
    {
        btnSearchToggle.Content = btnSearchToggle.Content.ToString() == "Patient Search"
            ? "Provider Search"
            : "Patient Search";

        ClearUi();
    }

    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();

        ResetAllComponents();

        //spnlDetail.Visibility        = Visibility.Collapsed;
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