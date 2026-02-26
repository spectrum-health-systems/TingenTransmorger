// 260226_code
// 260226_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.MeetingDetails partial class contains logic related to displaying meeting details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary>Handles the selection changed event for the meetings DataGrid.</summary>
    private void MeetingSelected()
    {
        /* Don't process selection if database is not yet initialized
         * TODO: Move
         */
        if (TmDb == null)
        {
            spnlMeetingDetail.Visibility = Visibility.Collapsed;

            return;
        }

        /* Verify the selected DataGrid item is a valid PatientMeetingRow with a non-empty MeetingId.  If the selection
         * is invalid or the MeetingId is missing, collapse the meeting details panel and exit early.
         * TODO: Move
         */
        if (dgrdMeetingList.SelectedItem is not MeetingRow selectedMeeting || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetail.Visibility = Visibility.Collapsed;

            return;
        }

        JsonElement? meetingDetail = TmDb.GetMeetingDetail(selectedMeeting.MeetingId);

        /* If the meeting detail could not be retrieved from the database, collapse the meeting details panel and exit
         * early.
         * TODO: Move
         */
        if (meetingDetail == null)
        {
            spnlMeetingDetail.Visibility = Visibility.Collapsed;

            return;
        }

        /* Extract generic meeting detail properties. Use MeetingId directly from selectedMeeting since we already have it
         */
        var meetingId = selectedMeeting.MeetingId;

        //DisplayGeneralDetails(selectedMeeting, meetingDetail, meetingId);

        //ClearUi();

        // Show/hide patient-specific and provider-specific meeting details based on current view mode
        // If we're viewing a provider, hide the patient-specific section and show provider section
        if (lblUserTypeKey.Content?.ToString() == "PROVIDER")
        {
            //spnlMeetingDetailsComponents.Visibility = Visibility.Visible;
            brdrGeneralMeetingDetail.Visibility  = Visibility.Visible;
            brdrProviderMeetingDetail.Visibility = Visibility.Visible;

            DisplayGeneralDetails(selectedMeeting, meetingDetail, meetingId);

            //brdrMeetingDetailsPatientContainer.Visibility  = Visibility.Collapsed;
            //brdrMeetingDetailsProviderContainer.Visibility = Visibility.Visible;

            //// Get and display participant names from MeetingDetail
            //var participantNames = string.Empty;

            //if (meetingDetail.Value.TryGetProperty("ParticipantNames", out var participantNamesElem))
            //{
            //    participantNames = participantNamesElem.GetString() ?? string.Empty;
            //}

            //txtProviderParticipantNames.Text = ReplaceNullValues(participantNames);
        }
        else
        {
            //spnlMeetingDetailsComponents.Visibility = Visibility.Visible;
            brdrGeneralMeetingDetail.Visibility = Visibility.Visible;
            brdrPatientMeetingDetail.Visibility = Visibility.Visible;

            DisplayGeneralDetails(selectedMeeting, meetingDetail, meetingId);
            DisplayPatientMeetingDetails(selectedMeeting);

        }

        // Show the meeting details section
        spnlMeetingDetail.Visibility = Visibility.Visible;
    }


    /// <summary></summary>
    /// <param name="selectedMeeting"></param>
    /// <param name="meetingDetail"></param>
    /// <param name="meetingId"></param>
    private void DisplayGeneralDetails(MeetingRow selectedMeeting, JsonElement? meetingDetail, string meetingId)
    {
        /* This is pretty dense, but it does cut down on the amount of repetitive code, and the underlying logic is
         * fairly simple:
         *
         * The <c>whatever.Text</c> value is set to the result of the <c>propertyName</c> if it exists, and converted
         * to "---" if it does not.
         */
        txbkMeetingIdValue.Text                   = ReplaceNullValues(meetingId);
        txbkMeetingStatusValue.Text               = ReplaceNullValues(GetStringProperty("Status", meetingDetail));
        txbkMeetingTitleValue.Text                = ReplaceNullValues(GetStringProperty("MeetingTitle", meetingDetail));
        txbkMeetingStartedByValue.Text            = ReplaceNullValues(GetStringProperty("InitiatedBy", meetingDetail));
        txbkMeetingScheduledStartValue.Text       = ReplaceNullValues(GetStringProperty("ScheduledStart", meetingDetail));
        txbkMeetingActualStartValue.Text          = ReplaceNullValues(GetStringProperty("ActualStart", meetingDetail));
        txbkMeetingScheduledEndValue.Text         = ReplaceNullValues(GetStringProperty("ScheduledEnd", meetingDetail));
        txbkMeetingActualEndValue.Text            = ReplaceNullValues(GetStringProperty("ActualEnd", meetingDetail));
        txbkMeetingEndedByValue.Text              = ReplaceNullValues(GetStringProperty("EndedBy", meetingDetail));
        txbkMeetingJoinsValue.Text                = ReplaceNullValues(GetStringProperty("Joins", meetingDetail));
        txbkMeetingDurationValue.Text             = ReplaceNullValues(GetStringProperty("Duration", meetingDetail));
        txbkMeetingServiceCodeValue.Text          = ReplaceNullValues(GetStringProperty("ServiceCode", meetingDetail));
        txbkMeetingWorkflowValue.Text             = ReplaceNullValues(GetStringProperty("Workflow", meetingDetail));
        txbkMeetingProgram.Text                   = ReplaceNullValues(GetStringProperty("Program", meetingDetail));
        txbkMeetingCheckedInByFrontDeskValue.Text = ReplaceNullValues(GetStringProperty("CheckedInByFrontDesk", meetingDetail));

        /* Display the meeting error, if it exists.
         */
        DisplayMeetingError(selectedMeeting);
    }




    private void DisplayProviderMeetingResults(string providerName)
    {
        var meetingRows = new List<MeetingRow>();

        // var providerDetails = TmDb.GetProviderDetails(_currentProviderName);
        var providerDetails = TmDb.GetProviderDetails(providerName);

        if (providerDetails != null && providerDetails.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meetingIdElement in meetingsArray.EnumerateArray())
                {
                    var meetingId = meetingIdElement.GetString();

                    if (string.IsNullOrWhiteSpace(meetingId))
                    {
                        continue;
                    }

                    var meetingDetail  = TmDb.GetMeetingDetail(meetingId);
                    var scheduledStart = string.Empty;
                    var actualStart    = string.Empty;
                    var scheduledEnd   = string.Empty;
                    var actualEnd      = string.Empty;
                    var status         = string.Empty;
                    var duration       = string.Empty;

                    if (meetingDetail != null)
                    {
                        scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
                            ? startElem.GetString()
                            : string.Empty;

                        actualStart = meetingDetail.Value.TryGetProperty("ActualStart", out var actualStartElem)
                            ? actualStartElem.GetString()
                            : string.Empty;

                        scheduledEnd = meetingDetail.Value.TryGetProperty("ScheduledEnd", out var scheduledEndElem)
                            ? scheduledEndElem.GetString()
                            : string.Empty;

                        actualEnd = meetingDetail.Value.TryGetProperty("ActualEnd", out var actualEndElem)
                            ? actualEndElem.GetString()
                            : string.Empty;

                        status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
                            ? statusElem.GetString()
                            : string.Empty;

                        duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
                            ? durationElem.GetString()
                            : string.Empty;
                    }

                    var hasError    = TmDb.HasMeetingError(meetingId);
                    var statusLower = status?.ToLower() ?? string.Empty;
                    var isCancelled = statusLower.Contains("cancel");
                    var isCompleted = statusLower.Contains("complete");

                    meetingRows.Add(new MeetingRow
                    {
                        MeetingId    = meetingId,
                        Start        = ReplaceNullValues(scheduledStart ?? string.Empty),
                        ActualStart  = ReplaceNullValues(actualStart   ?? string.Empty),
                        ScheduledEnd = ReplaceNullValues(scheduledEnd  ?? string.Empty),
                        ActualEnd    = ReplaceNullValues(actualEnd     ?? string.Empty),
                        Duration     = ReplaceNullValues(duration      ?? string.Empty),
                        Status       = ReplaceNullValues(status        ?? string.Empty),
                        HasError     = hasError,
                        IsCancelled  = isCancelled,
                        IsCompleted  = isCompleted
                    });
                }
            }
        }

        meetingRows = meetingRows.OrderByDescending(m => m.Start).ToList();

        var totalCount     = meetingRows.Count;
        var completedCount = meetingRows.Count(m => m.IsCompleted);
        var cancelledCount = meetingRows.Count(m => m.IsCancelled);

        var inProgressCount = 0;
        var expiredCount    = 0;
        var scheduledCount  = 0;

        foreach (var meeting in meetingRows)
        {
            var statusLower = meeting.Status?.ToLower() ?? string.Empty;

            if (meeting.IsCompleted || meeting.IsCancelled)
            {
                continue;
            }

            if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
            {
                inProgressCount++;
            }
            else if (statusLower.Contains("expired"))
            {
                expiredCount++;
            }
            else if (statusLower.Contains("scheduled"))
            {
                scheduledCount++;
            }
        }

        txbkTotalMeetingsValue.Text      = $"{totalCount} MEETINGS";
        txbkCompletedMeetingsValue.Text  = $"{completedCount} Completed";
        txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        txbkMeetingsExpiredValue.Text    = $"{expiredCount} Expired";
        txbkMeetingsCancelledValue.Text  = $"{cancelledCount} Cancelled";
        txbkMeetingsScheduledValue.Text  = $"{scheduledCount} Scheduled";

        dgrdMeetingList.ItemsSource = meetingRows;

        spnlMeetingDetail.Visibility = meetingRows.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        spnlMeetingDetail.Visibility = Visibility.Collapsed;
    }

    private void DisplayPatientMeetingResults(JsonElement? patientDetails)
    {
        // Display meetings
        var meetingRows = new List<MeetingRow>();

        if (patientDetails.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meeting in meetingsArray.EnumerateArray())
                {
                    // Get MeetingId from Patients.Meetings
                    var meetingId = meeting.TryGetProperty("MeetingId", out var meetingIdElem)
                        ? meetingIdElem.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(meetingId))
                    {
                        continue;
                    }

                    // Get Arrived, Dropped, Duration from Patients.Meetings
                    var arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                        ? arrivedElem.GetString()
                        : string.Empty;

                    var dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                        ? droppedElem.GetString()
                        : string.Empty;

                    var duration = meeting.TryGetProperty("Duration", out var durationElem)
                        ? (durationElem.GetString() ?? string.Empty)
                        : string.Empty;

                    // Get ScheduledStart, ActualStart, ScheduledEnd, ActualEnd, and Status from MeetingDetail
                    var meetingDetail  = TmDb.GetMeetingDetail(meetingId);
                    var scheduledStart = string.Empty;
                    var actualStart    = string.Empty;
                    var scheduledEnd   = string.Empty;
                    var actualEnd      = string.Empty;
                    var status         = string.Empty;

                    if (meetingDetail != null)
                    {
                        scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
                            ? startElem.GetString()
                            : string.Empty;

                        actualStart = meetingDetail.Value.TryGetProperty("ActualStart", out var actualStartElem)
                            ? actualStartElem.GetString()
                            : string.Empty;

                        scheduledEnd = meetingDetail.Value.TryGetProperty("ScheduledEnd", out var scheduledEndElem)
                            ? scheduledEndElem.GetString()
                            : string.Empty;

                        actualEnd = meetingDetail.Value.TryGetProperty("ActualEnd", out var actualEndElem)
                            ? actualEndElem.GetString()
                            : string.Empty;

                        status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
                            ? statusElem.GetString()
                            : string.Empty;
                    }

                    // Replace any occurrence of "null" (case-insensitive) with a single "---"
                    string ReplaceNull(string value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return "";
                        }

                        // Replace all occurrences of "null" (case-insensitive) with a placeholder
                        var result = System.Text.RegularExpressions.Regex.Replace(value, @"\bnull\b", "<<NULL>>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        // Check if we had any replacements
                        if (result.Contains("<<NULL>>"))
                        {
                            // If the entire string is just null markers (with possible whitespace/separators), return single "---"
                            var cleanedResult = result.Replace("<<NULL>>", "").Trim().Trim(',').Trim(';').Trim();
                            if (string.IsNullOrWhiteSpace(cleanedResult))
                            {
                                return "---";
                            }

                            // Otherwise replace all null markers with "---"
                            result = result.Replace("<<NULL>>", "---");
                        }

                        return string.IsNullOrWhiteSpace(result) ? "---" : result;
                    }

                    // Check if meeting has an error
                    var hasError = TmDb.HasMeetingError(meetingId);

                    // Check status flags (case-insensitive)
                    var statusLower = status?.ToLower() ?? string.Empty;
                    var isCancelled = statusLower.Contains("cancel");
                    var isCompleted = statusLower.Contains("complete");

                    meetingRows.Add(new MeetingRow
                    {
                        MeetingId    = meetingId,
                        Start        = ReplaceNull(scheduledStart ?? string.Empty),
                        ActualStart  = ReplaceNull(actualStart    ?? string.Empty),
                        ScheduledEnd = ReplaceNull(scheduledEnd   ?? string.Empty),
                        ActualEnd    = ReplaceNull(actualEnd      ?? string.Empty),
                        Duration     = ReplaceNull(duration       ?? string.Empty),
                        Status       = ReplaceNull(status         ?? string.Empty),
                        HasError     = hasError,
                        IsCancelled  = isCancelled,
                        IsCompleted  = isCompleted
                    });
                }
            }
        }

        // Sort meetings by ScheduledStart descending (most recent first)
        meetingRows = meetingRows.OrderByDescending(m => m.Start).ToList();

        // Count meetings by status
        var totalCount     = meetingRows.Count;
        var completedCount = meetingRows.Count(m => m.IsCompleted);
        var cancelledCount = meetingRows.Count(m => m.IsCancelled);

        // Count In-Progress, Expired, and Scheduled
        var inProgressCount = 0;
        var expiredCount    = 0;
        var scheduledCount  = 0;

        foreach (var meeting in meetingRows)
        {
            var statusLower = meeting.Status?.ToLower() ?? string.Empty;

            // Skip already counted statuses
            if (meeting.IsCompleted || meeting.IsCancelled)
            {
                continue;
            }

            if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
            {
                inProgressCount++;
            }
            else if (statusLower.Contains("expired"))
            {
                expiredCount++;
            }
            else if (statusLower.Contains("scheduled"))
            {
                scheduledCount++;
            }
        }

        // Update the header with the detailed count using individual TextBlocks
        txbkTotalMeetingsValue.Text      = $"{totalCount} MEETINGS";
        txbkCompletedMeetingsValue.Text  = $"{completedCount} Completed";
        txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        txbkMeetingsExpiredValue.Text    = $"{expiredCount} Expired";
        txbkMeetingsCancelledValue.Text  = $"{cancelledCount} Cancelled";
        txbkMeetingsScheduledValue.Text  = $"{scheduledCount} Scheduled";

        // Bind to DataGrid
        dgrdMeetingList.ItemsSource = meetingRows;

        // Show meetings section if there are meetings
        spnlMeetingDetail.Visibility = meetingRows.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Hide meeting details until a meeting is selected
        spnlMeetingDetail.Visibility = Visibility.Collapsed;
    }



    /*
     * me
     */


    private void DisplayProviderMeetingDetails()
    {

    }

    private void DisplayPatientMeetingDetails(MeetingRow selectedMeeting)
    {
        // Get and display participant meeting quality data from Patients.Meetings
        var qualityData     = string.Empty;
        var arrived         = string.Empty;
        var dropped         = string.Empty;
        var patientDuration = string.Empty;
        var rating          = string.Empty;
        var checkInViaChat  = string.Empty;
        var checkInWait     = string.Empty;
        var waitForCareTeam = string.Empty;
        var waitForProvider = string.Empty;
        var checkOutWait    = string.Empty;
        var device          = string.Empty;
        var os              = string.Empty;
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
                        ? mtgIdElem.GetString()
                        : null;

                    if (mtgId == selectedMeeting.MeetingId)
                    {
                        // Get all the patient meeting data
                        qualityData = meeting.TryGetProperty("QualityData", out var qualityDataElem)
                            ? (qualityDataElem.GetString() ?? string.Empty)
                            : string.Empty;

                        arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                            ? (arrivedElem.GetString() ?? string.Empty)
                            : string.Empty;

                        dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                            ? (droppedElem.GetString() ?? string.Empty)
                            : string.Empty;

                        patientDuration = meeting.TryGetProperty("Duration", out var patientDurationElem)
                            ? (patientDurationElem.GetString() ?? string.Empty)
                            : string.Empty;

                        rating = meeting.TryGetProperty("Rating", out var ratingElem)
                            ? (ratingElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkInViaChat = meeting.TryGetProperty("CheckInViaChat", out var checkInViaChatElem)
                            ? (checkInViaChatElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkInWait = meeting.TryGetProperty("CheckInWait", out var checkInWaitElem)
                            ? (checkInWaitElem.GetString() ?? string.Empty)
                            : string.Empty;

                        waitForCareTeam = meeting.TryGetProperty("WaitForCareTeamMember", out var waitForCareTeamElem)
                            ? (waitForCareTeamElem.GetString() ?? string.Empty)
                            : string.Empty;

                        waitForProvider = meeting.TryGetProperty("WaitForProvider", out var waitForProviderElem)
                            ? (waitForProviderElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkOutWait = meeting.TryGetProperty("CheckOutWait", out var checkOutWaitElem)
                            ? (checkOutWaitElem.GetString() ?? string.Empty)
                            : string.Empty;

                        device = meeting.TryGetProperty("Device", out var deviceElem)
                            ? (deviceElem.GetString() ?? string.Empty)
                            : string.Empty;

                        os = meeting.TryGetProperty("Os", out var osElem)
                            ? (osElem.GetString() ?? string.Empty)
                            : string.Empty;

                        browser = meeting.TryGetProperty("Browser", out var browserElem)
                            ? (browserElem.GetString() ?? string.Empty)
                            : string.Empty;

                        break;
                    }
                }
            }
        }

        // Populate patient meeting detail fields
        txbkPatientArrivedValue.Text     = ReplaceNullValues(arrived);
        txbkPatientDroppedValue.Text     = ReplaceNullValues(dropped);
        txbkPatientDurationValue.Text    = ReplaceNullValues(patientDuration);
        txbkPatientRatingValue.Text      = ReplaceNullValues(rating);
        txbkCheckedInViaChatValue.Text   = ReplaceNullValues(checkInViaChat);
        txbkCheckInWaitValue.Text        = ReplaceNullValues(checkInWait);
        txbkWaitForCareTeamValue.Text    = ReplaceNullValues(waitForCareTeam);
        txbkWaitForProviderValue.Text    = ReplaceNullValues(waitForProvider);
        txbkCheckOutWaitValue.Text       = ReplaceNullValues(checkOutWait);
        txbkPatientDeviceValue.Text      = ReplaceNullValues(device);
        txbkPatientOsValue.Text          = ReplaceNullValues(os);
        txbkPatientBrowserValue.Text     = ReplaceNullValues(browser);
        txbkPatientMeetingQualityDataValue.Text = ReplaceNullValues(qualityData);
    }


    private void DisplayMeetingError(MeetingRow selectedMeeting)
    {
        // Get and display meeting error if it exists
        var meetingError = TmDb.GetMeetingError(selectedMeeting.MeetingId);

        if (meetingError != null)
        {
            var kind = meetingError.Value.TryGetProperty("Kind", out var kindElem)
                ? (kindElem.GetString() ?? string.Empty)
                : string.Empty;

            var reason = meetingError.Value.TryGetProperty("Reason", out var reasonElem)
                ? (reasonElem.GetString() ?? string.Empty)
                : string.Empty;

            txbkMeetingErrorValue.Text = (!string.IsNullOrWhiteSpace(kind) || !string.IsNullOrWhiteSpace(reason))
                ? $"{kind}\n{reason}"
                : "---";
        }
        else
        {
            txbkMeetingErrorValue.Text = "---";
        }
    }


    private static string GetStringProperty(string propertyName, JsonElement? meetingDetail)
    {
        return meetingDetail.Value.TryGetProperty(propertyName, out var elem)
            ? (elem.GetString() ?? string.Empty)
            : string.Empty;
    }

    private static string ReplaceNullValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "---";
        }

        var result = System.Text.RegularExpressions.Regex.Replace(value, @"\bnull\b", "<<NULL>>",  System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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
}