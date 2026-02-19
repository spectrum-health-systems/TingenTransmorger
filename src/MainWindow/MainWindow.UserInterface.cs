// 260219_code
// 260219_documentation

using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using TingenTransmorger.Models;

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

    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty;
        lstbxSearchResults.Items.Clear();
        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility                 = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility          = Visibility.Collapsed;
    }

    /// <summary>Modifies the search results based on the current search type and search text.</summary>
    /// <param name="searchType">The type of search.</param>
    /// <param name="searchText">Contents of the search box.</param>
    private void ModifySearchResults(string searchType, string searchText)
    {
        var searchResults = GetSearchResults(searchType, searchText);
        DisplaySearchResults(searchResults);
    }

    /// <summary>Get a list of patient/provider search results.</summary>
    /// <param name="searchType">The type of search.</param>
    /// <param name="searchText">Contents of the search box.</param>
    /// <returns>The search results.</returns>
    private List<string> GetSearchResults(string searchType, string searchText)
    {
        if (string.IsNullOrWhiteSpace(txbxSearchBox.Text))
        {
            return [];
        }

        /* If the search box contains only an asterisk, treat it as a wildcard to return all results
         */
        if (txbxSearchBox.Text == "*")
        {
            searchText = string.Empty;
        }

        return searchType.Contains("patient", StringComparison.OrdinalIgnoreCase)
            ? rbtnSearchByName.IsChecked == true
                ? Database.SearchFor.PatientByName(searchText, TmDb)
                : Database.SearchFor.PatientById(searchText, TmDb)
            : searchType.Contains("provider", StringComparison.OrdinalIgnoreCase)
                ? rbtnSearchByName.IsChecked == true
                            ? Database.SearchFor.ProviderByName(searchText, TmDb)
                            : Database.SearchFor.ProviderById(searchText, TmDb)
                : [];
    }

    /// <summary>Display search results.</summary>
    /// <param name="searchResults">The list of search results to display.</param>
    private void DisplaySearchResults(List<string> searchResults)
    {
        lstbxSearchResults.Items.Clear();

        if (searchResults.Count == 0)
        {
            lstbxSearchResults.Items.Add("No results found.");
        }
        else
        {
            foreach (string result in searchResults)
            {
                lstbxSearchResults.Items.Add(result);
            }
        }
    }



    private void DisplayDetails(string searchMode, string selectedItem)
    {
        /* This is here so we don't hit a weird loop with ClearUi().
        */
        if (lstbxSearchResults.Items.Count == 0)
        {
            return;
        }

        var lastParenIndex = selectedItem.LastIndexOf('(');
        var name           = selectedItem.Substring(0, lastParenIndex).Trim();
        var id             = selectedItem.Substring(lastParenIndex + 1).TrimEnd(')').Trim();

        switch (btnSearchToggle.Content.ToString())
        {
            case "Patient Search":
                DisplayPatientDetails(name, id);
                break;

            case "Provider Search":
                DisplayProviderDetails(name, id);
                break;
        }
    }



    private void ShowPhoneDetails()
    {
        var messageHistoryWindow = new Database.MessageHistoryWindow(_smsFailures, _smsDeliveries)
        {
            Owner = this
        };

        messageHistoryWindow.ShowDialog();
    }


    /// <summary>Handles the selection changed event for the meetings DataGrid.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user selects a meeting from the meetings table.
    /// It retrieves the full meeting details and displays them in the details section.
    /// </para>
    /// </remarks>
    private void MeetingSelected()
    {
        // Don't process selection if database is not yet initialized
        if (TmDb == null)
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Get the selected meeting
        var selectedMeeting = dgrdMeetingResults.SelectedItem as PatientMeetingRow;
        if (selectedMeeting == null || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Get meeting details from database
        var meetingDetail = TmDb.GetMeetingDetail(selectedMeeting.MeetingId);
        if (meetingDetail == null)
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Helper function to replace null values
        string ReplaceNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "---";

            var result = System.Text.RegularExpressions.Regex.Replace(
                value,
                @"\bnull\b",
                "<<NULL>>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (result.Contains("<<NULL>>"))
            {
                var cleanedResult = result.Replace("<<NULL>>", "").Trim().Trim(',').Trim(';').Trim();
                if (string.IsNullOrWhiteSpace(cleanedResult))
                {
                    return "---";
                }

                result = result.Replace("<<NULL>>", "---");
            }

            return string.IsNullOrWhiteSpace(result) ? "---" : result;
        }

        // Extract meeting detail properties
        // Use MeetingId directly from selectedMeeting since we already have it
        var meetingId = selectedMeeting.MeetingId;
        var status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
            ? (statusElem.GetString() ?? string.Empty) : string.Empty;
        var initiatedBy = meetingDetail.Value.TryGetProperty("InitiatedBy", out var initiatedByElem)
            ? (initiatedByElem.GetString() ?? string.Empty) : string.Empty;
        var scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var scheduledStartElem)
            ? (scheduledStartElem.GetString() ?? string.Empty) : string.Empty;
        var actualStart = meetingDetail.Value.TryGetProperty("ActualStart", out var actualStartElem)
            ? (actualStartElem.GetString() ?? string.Empty) : string.Empty;
        var scheduledEnd = meetingDetail.Value.TryGetProperty("ScheduledEnd", out var scheduledEndElem)
            ? (scheduledEndElem.GetString() ?? string.Empty) : string.Empty;
        var actualEnd = meetingDetail.Value.TryGetProperty("ActualEnd", out var actualEndElem)
            ? (actualEndElem.GetString() ?? string.Empty) : string.Empty;
        var endedBy = meetingDetail.Value.TryGetProperty("EndedBy", out var endedByElem)
            ? (endedByElem.GetString() ?? string.Empty) : string.Empty;
        var joins = meetingDetail.Value.TryGetProperty("Joins", out var joinsElem)
            ? (joinsElem.GetString() ?? string.Empty) : string.Empty;
        var duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
            ? (durationElem.GetString() ?? string.Empty) : string.Empty;
        var meetingTitle = meetingDetail.Value.TryGetProperty("MeetingTitle", out var meetingTitleElem)
            ? (meetingTitleElem.GetString() ?? string.Empty) : string.Empty;
        var serviceCode = meetingDetail.Value.TryGetProperty("ServiceCode", out var serviceCodeElem)
            ? (serviceCodeElem.GetString() ?? string.Empty) : string.Empty;


        // Extract workflow, program, and front desk check-in
        var workflow = meetingDetail.Value.TryGetProperty("Workflow", out var workflowElem)
            ? (workflowElem.GetString() ?? string.Empty) : string.Empty;
        var program = meetingDetail.Value.TryGetProperty("Program", out var programElem)
            ? (programElem.GetString() ?? string.Empty) : string.Empty;
        var checkedInByFrontDesk = meetingDetail.Value.TryGetProperty("CheckedInByFrontDesk", out var checkedInElem)
            ? (checkedInElem.GetString() ?? string.Empty) : string.Empty;

        // Populate labels with null-safe values
        txbkMeetingIdValue.Text = ReplaceNull(meetingId ?? string.Empty);
        txbkMeetingStatusValue.Text = ReplaceNull(status ?? string.Empty);
        txbkMeetingTitleValue.Text = ReplaceNull(meetingTitle ?? string.Empty);

        // Populate meeting detail TextBlocks
        txbkMeetingStartedByValue.Text = ReplaceNull(initiatedBy ?? string.Empty);
        txbkMeetingScheduledStartValue.Text = ReplaceNull(scheduledStart ?? string.Empty);
        txbkMeetingActualStartValue.Text = ReplaceNull(actualStart ?? string.Empty);
        txbkMeetingScheduledEndValue.Text = ReplaceNull(scheduledEnd ?? string.Empty);
        txbkMeetingActualEndValue.Text = ReplaceNull(actualEnd ?? string.Empty);
        txbkMeetingEndedByValue.Text = ReplaceNull(endedBy ?? string.Empty);
        txbkMeetingJoins.Text = ReplaceNull(joins ?? string.Empty);
        txbkMeetingDurationValue.Text = ReplaceNull(duration ?? string.Empty);
        txbkMeetingServiceCodeValue.Text = ReplaceNull(serviceCode ?? string.Empty);

        // Populate additional information TextBlocks
        txbkMeetingWorkflowValue.Text = ReplaceNull(workflow ?? string.Empty);
        txbkMeetingProgram.Text = ReplaceNull(program ?? string.Empty);
        txbkMeetingCheckedInByFrontDeskValue.Text = ReplaceNull(checkedInByFrontDesk ?? string.Empty);

        // Get and display meeting error if it exists
        var meetingError = TmDb.GetMeetingError(selectedMeeting.MeetingId);
        if (meetingError != null)
        {
            var kind = meetingError.Value.TryGetProperty("Kind", out var kindElem)
                ? (kindElem.GetString() ?? string.Empty) : string.Empty;
            var reason = meetingError.Value.TryGetProperty("Reason", out var reasonElem)
                ? (reasonElem.GetString() ?? string.Empty) : string.Empty;

            if (!string.IsNullOrWhiteSpace(kind) || !string.IsNullOrWhiteSpace(reason))
            {
                txbkMeetingErrorValue.Text = $"{kind}\n{reason}";
            }
            else
            {
                txbkMeetingErrorValue.Text = "---";
            }
        }
        else
        {
            txbkMeetingErrorValue.Text = "---";
        }

        // Get and display participant meeting quality data from Patients.Meetings
        var qualityData = string.Empty;
        var arrived = string.Empty;
        var dropped = string.Empty;
        var patientDuration = string.Empty;
        var rating = string.Empty;
        var checkInViaChat = string.Empty;
        var checkInWait = string.Empty;
        var waitForCareTeam = string.Empty;
        var waitForProvider = string.Empty;
        var checkOutWait = string.Empty;
        var device = string.Empty;
        var os = string.Empty;
        var browser = string.Empty;

        // Retrieve the patient details to access the meetings array
        var patientDetailsForQuality = TmDb.GetPatientDetails(_currentPatientName, _currentPatientId);

        if (patientDetailsForQuality != null && patientDetailsForQuality.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meeting in meetingsArray.EnumerateArray())
                {
                    var mtgId = meeting.TryGetProperty("MeetingId", out var mtgIdElem)
                        ? mtgIdElem.GetString() : null;

                    if (mtgId == selectedMeeting.MeetingId)
                    {
                        // Get all the patient meeting data
                        qualityData = meeting.TryGetProperty("QualityData", out var qualityDataElem)
                            ? (qualityDataElem.GetString() ?? string.Empty) : string.Empty;
                        arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                            ? (arrivedElem.GetString() ?? string.Empty) : string.Empty;
                        dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                            ? (droppedElem.GetString() ?? string.Empty) : string.Empty;
                        patientDuration = meeting.TryGetProperty("Duration", out var patientDurationElem)
                            ? (patientDurationElem.GetString() ?? string.Empty) : string.Empty;
                        rating = meeting.TryGetProperty("Rating", out var ratingElem)
                            ? (ratingElem.GetString() ?? string.Empty) : string.Empty;
                        checkInViaChat = meeting.TryGetProperty("CheckInViaChat", out var checkInViaChatElem)
                            ? (checkInViaChatElem.GetString() ?? string.Empty) : string.Empty;
                        checkInWait = meeting.TryGetProperty("CheckInWait", out var checkInWaitElem)
                            ? (checkInWaitElem.GetString() ?? string.Empty) : string.Empty;
                        waitForCareTeam = meeting.TryGetProperty("WaitForCareTeamMember", out var waitForCareTeamElem)
                            ? (waitForCareTeamElem.GetString() ?? string.Empty) : string.Empty;
                        waitForProvider = meeting.TryGetProperty("WaitForProvider", out var waitForProviderElem)
                            ? (waitForProviderElem.GetString() ?? string.Empty) : string.Empty;
                        checkOutWait = meeting.TryGetProperty("CheckOutWait", out var checkOutWaitElem)
                            ? (checkOutWaitElem.GetString() ?? string.Empty) : string.Empty;
                        device = meeting.TryGetProperty("Device", out var deviceElem)
                            ? (deviceElem.GetString() ?? string.Empty) : string.Empty;
                        os = meeting.TryGetProperty("Os", out var osElem)
                            ? (osElem.GetString() ?? string.Empty) : string.Empty;
                        browser = meeting.TryGetProperty("Browser", out var browserElem)
                            ? (browserElem.GetString() ?? string.Empty) : string.Empty;
                        break;
                    }
                }
            }
        }

        // Populate patient meeting detail fields
        txbkPatientArrivedValue.Text = ReplaceNull(arrived);
        txbkPatientDroppedValue.Text = ReplaceNull(dropped);
        txbkPatientDurationValue.Text = ReplaceNull(patientDuration);
        txbkPatientRatingValue.Text = ReplaceNull(rating);
        txbkCheckedInViaChatValue.Text = ReplaceNull(checkInViaChat);
        txbkCheckInWaitValue.Text = ReplaceNull(checkInWait);
        txbkWaitForCareTeamValue.Text = ReplaceNull(waitForCareTeam);
        txbkWaitForProviderValue.Text = ReplaceNull(waitForProvider);
        txbkCheckOutWaitValue.Text = ReplaceNull(checkOutWait);
        txbkPatientDeviceValue.Text = ReplaceNull(device);
        txbkPatientOsValue.Text = ReplaceNull(os);
        txbkPatientBrowserValue.Text = ReplaceNull(browser);
        txbkMeetingQualityDataValue.Text = ReplaceNull(qualityData);

        // Show/hide patient-specific and provider-specific meeting details based on current view mode
        // If we're viewing a provider, hide the patient-specific section and show provider section
        if (lblPatientProviderKey.Content?.ToString() == "PROVIDER")
        {
            brdrMeetingDetailsPatientContainer.Visibility = Visibility.Collapsed;
            brdrMeetingDetailsProviderContainer.Visibility = Visibility.Visible;

            // Get and display participant names from MeetingDetail
            var participantNames = string.Empty;
            if (meetingDetail.Value.TryGetProperty("ParticipantNames", out var participantNamesElem))
            {
                participantNames = participantNamesElem.GetString() ?? string.Empty;
            }
            txtProviderParticipantNames.Text = ReplaceNull(participantNames);
        }
        else
        {
            brdrMeetingDetailsPatientContainer.Visibility = Visibility.Visible;
            brdrMeetingDetailsProviderContainer.Visibility = Visibility.Collapsed;
        }

        // Show the meeting details section
        spnlMeetingDetailsComponents.Visibility = Visibility.Visible;
    }



    /// <summary>Updates the btnPhoneDetails button appearance based on SMS failure and delivery records.</summary>
    private void UpdateDetailsButtonColor(bool hasFailures, bool hasDeliveries, Button theButton)
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


    /// <summary>Handles the email details button click event.</summary>
    private void ShowEmailDetails()
    {
        var emailHistoryWindow = new Database.MessageHistoryWindow(_emailFailures, _emailDeliveries)
        {
            Owner = this
        };

        emailHistoryWindow.ShowDialog();
    }





    private void SetupPatientDetailUi(string patientName, string patientId)
    {

        lblPatientProviderKey.Content      = "PATIENT";
        lblPatientProviderNameValue.Content   = patientName;
        lblPatientProviderIdValue.Content     = patientId;
        spnlPatientProviderDetailsComponents.Visibility = Visibility.Visible;
        spnlPatientPhoneComponents.Visibility   = Visibility.Visible;
        spnlPatientEmailComponents.Visibility   = Visibility.Visible;
    }



    private void SetSearchToggleContent(string buttonContent)
    {
        switch (buttonContent)
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


}