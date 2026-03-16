// 260227_code
// 260311_documentation

using System.Text;
using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.DataCopy partial class contains logic related to copying meeting details to the clipboard.
 */
public partial class MainWindow : Window
{
    /// <summary>Copies general meeting details from the UI to the clipboard as formatted text.</summary>
    /// <remarks>Displays a confirmation on success or an error message if the clipboard operation fails.</remarks>
    private void CopyGeneralMeetingDetails()
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("    MEETING DETAILS");
            sb.AppendLine("    ---------------");
            sb.AppendLine("         Meeting ID: " + txbkMeetingIdValue.Text);
            sb.AppendLine("              Title: " + txbkMeetingTitleValue.Text);
            sb.AppendLine("             Status: " + txbkMeetingStatusValue.Text);
            sb.AppendLine("              Joins: " + txbkMeetingJoinsValue.Text);
            sb.AppendLine("           Duration: " + txbkMeetingDurationValue.Text);
            sb.AppendLine("       Service code: " + txbkMeetingServiceCodeValue.Text);
            sb.AppendLine("         Started by: " + txbkMeetingStartedByValue.Text);
            sb.AppendLine("    Scheduled start: " + txbkMeetingScheduledStartValue.Text);
            sb.AppendLine("       Actual start: " + txbkMeetingActualStartValue.Text);
            sb.AppendLine("           Ended by: " + txbkMeetingEndedByValue.Text);
            sb.AppendLine("      Scheduled end: " + txbkMeetingScheduledEndValue.Text);
            sb.AppendLine("         Actual end: " + txbkMeetingActualEndValue.Text);
            sb.AppendLine("           Workflow: " + txbkMeetingWorkflowValue.Text);
            sb.AppendLine("            Program: " + txbkMeetingProgram.Text);
            sb.AppendLine("Front Desk Check-In: " + txbkMeetingCheckedInByFrontDeskValue.Text);
            sb.AppendLine("      Meeting error: " + txbkMeetingErrorValue.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Copies patient participant details from the UI to the clipboard as formatted text.</summary>
    /// <remarks>Displays a confirmation on success or an error message if the clipboard operation fails.</remarks>
    private void CopyPatientMeetingDetails()
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("    MEETING DETAILS");
            sb.AppendLine("    ---------------");
            sb.AppendLine("    Patient arrived: " + txbkPatientArrivedValue.Text);
            sb.AppendLine("    Patient dropped: " + txbkPatientDroppedValue.Text);
            sb.AppendLine("           Duration: " + txbkPatientDurationValue.Text);
            sb.AppendLine("             Rating: " + txbkPatientRatingValue.Text);
            sb.AppendLine("Checked-In via chat: " + txbkCheckedInViaChatValue.Text);
            sb.AppendLine("      Check-In wait: " + txbkCheckInWaitValue.Text);
            sb.AppendLine(" Wait for Care Team: " + txbkWaitForCareTeamValue.Text);
            sb.AppendLine("  Wait for provider: " + txbkWaitForProviderValue.Text);
            sb.AppendLine("     Check-out wait: " + txbkCheckOutWaitValue.Text);
            sb.AppendLine("             Device: " + txbkPatientDeviceValue.Text);
            sb.AppendLine("                 OS: " + txbkPatientOsValue.Text);
            sb.AppendLine("            Browser: " + txbkPatientBrowserValue.Text);
            sb.AppendLine("       Quality Data: " + txbkPatientMeetingQualityDataValue.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Copies provider participant details from the UI to the clipboard as formatted text.</summary>
    /// <remarks>Displays a confirmation on success or an error message if the clipboard operation fails.</remarks>
    private void CopyProviderMeetingDetails()
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("    MEETING DETAILS");
            sb.AppendLine("    ---------------");
            sb.AppendLine("  Participant Names: " + txbkProviderParticipantNames.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}