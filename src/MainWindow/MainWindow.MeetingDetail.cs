// 260227_code
// 260311_documentation

/* Development note: This class needs to be refactored.
 */

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.MeetingDetails partial class contains logic related to displaying meeting details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary>Handles meeting selection, loading and displaying the appropriate detail view.</summary>
    /// <remarks>
    /// Collapses the detail panel and returns early if no meeting is selected or no detail record is found.<br/>
    /// <br/>
    /// Routes to provider or patient detail display based on the current user type label.
    /// </remarks>
    private void MeetingSelected()
    {
        if (dgrdMeetingList.SelectedItem is not MeetingRow selectedMeeting || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetail.Visibility = Visibility.Collapsed;

            return;
        }

        JsonElement? meetingDetail = _tmDb.GetMeetingDetail(selectedMeeting.MeetingId);

        if (meetingDetail == null)
        {
            spnlMeetingDetail.Visibility = Visibility.Collapsed;

            return;
        }

        if (lblUserTypeKey.Content?.ToString() == "PROVIDER")
        {
            DisplayGeneralDetails(selectedMeeting, meetingDetail, selectedMeeting.MeetingId);
            DisplayProviderMeetingDetails(selectedMeeting);

            spnlMeetingDetail.Visibility = Visibility.Visible;
        }
        else
        {
            DisplayGeneralDetails(selectedMeeting, meetingDetail, selectedMeeting.MeetingId);
            DisplayPatientMeetingDetails(selectedMeeting);

            spnlMeetingDetail.Visibility = Visibility.Visible;
        }
    }

    /// <summary>Populates general meeting detail UI fields from the selected meeting and its JSON record.</summary>
    /// <remarks>Also calls <see cref="DisplayMeetingError"/> to populate the error field.</remarks>
    /// <param name="selectedMeeting">The selected meeting row from the meeting list grid.</param>
    /// <param name="meetingDetail">The JSON element containing detailed meeting data.</param>
    /// <param name="meetingId">The meeting ID used to populate the ID field directly.</param>
    private void DisplayGeneralDetails(MeetingRow selectedMeeting, JsonElement? meetingDetail, string meetingId)
    {
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

        DisplayMeetingError(selectedMeeting);
    }

    /// <summary>Populates the meeting grid and status summary fields from the patient's JSON data.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Deserializes and enriches each meeting entry with data from the meeting detail database.</item>
    /// <item>Computes completed, cancelled, in-progress, expired, and scheduled meeting counts.</item>
    /// <item>Sorts meetings by scheduled start date descending before binding to the grid.</item>
    /// </list>
    /// </remarks>
    /// <param name="patientDetails">The JSON element containing the patient's meeting records.</param>
    private void DisplayPatientMeetingResults(JsonElement? patientDetails)
    {
        var meetingRows = new List<MeetingRow>();

        if (patientDetails.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meeting in meetingsArray.EnumerateArray())
                {
                    var meetingId = meeting.TryGetProperty("MeetingId", out var meetingIdElem)
                        ? meetingIdElem.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(meetingId))
                    {
                        continue;
                    }

                    var arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                        ? arrivedElem.GetString()
                        : string.Empty;

                    var dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                        ? droppedElem.GetString()
                        : string.Empty;

                    var duration = meeting.TryGetProperty("Duration", out var durationElem)
                        ? (durationElem.GetString() ?? string.Empty)
                        : string.Empty;

                    var meetingDetail  = _tmDb.GetMeetingDetail(meetingId);
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

                    string ReplaceNull(string value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return "";
                        }

                        var result = System.Text.RegularExpressions.Regex.Replace(value, @"\bnull\b", "<<NULL>>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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

                    var hasError = _tmDb.HasMeetingError(meetingId);

                    var statusLower = status?.ToLower() ?? string.Empty;
                    var isCancelled = statusLower.Contains("cancel");
                    var isCompleted = statusLower.Contains("complete");

                    meetingRows.Add(new MeetingRow
                    {
                        MeetingId    = meetingId,
                        ScheduledStart        = ReplaceNull(scheduledStart ?? string.Empty),
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

        meetingRows = meetingRows.OrderByDescending(m => m.ScheduledStart).ToList();

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

    /* Already refactored */

    /// <summary>Populates provider participant names in the UI for the selected meeting.</summary>
    /// <remarks>Splits names on semicolons or commas and displays '---' if no participants are found.</remarks>
    /// <param name="selectedMeeting">The selected meeting row whose participant names will be displayed.</param>
    private void DisplayProviderMeetingDetails(MeetingRow selectedMeeting)
    {
        var meetingDetail = _tmDb.GetMeetingDetail(selectedMeeting.MeetingId);

        var participantNamesRaw = meetingDetail != null
            ? GetStringProperty("ParticipantNames", meetingDetail)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(participantNamesRaw))
        {
            txbkProviderParticipantNames.Text = "---";
            return;
        }

        char delimiter    = participantNamesRaw.Contains(';') ? ';' : ',';
        var  parsedNames  = participantNamesRaw
            .Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(n => !string.IsNullOrWhiteSpace(n));

        txbkProviderParticipantNames.Text = string.Join(", ", parsedNames);
    }

    /// <summary>Populates patient-specific meeting detail fields in the UI for the selected meeting.</summary>
    /// <remarks>Searches patient meeting records in the database to find a match for the selected meeting.</remarks>
    /// <param name="selectedMeeting">The selected meeting row whose patient details will be displayed.</param>
    private void DisplayPatientMeetingDetails(MeetingRow selectedMeeting)
    {
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

        var patientDetails = _tmDb.GetPatientDetails(_currentPatientName, _currentPatientId);

        if (patientDetails != null && patientDetails.Value.TryGetProperty("Meetings", out var meetingsArray))
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


    /// <summary>Populates the meeting error field in the UI with the error kind and reason, if one exists.</summary>
    /// <remarks>Displays '---' if no error record exists or both Kind and Reason are empty strings.</remarks>
    /// <param name="selectedMeeting">The selected meeting row whose error data will be displayed.</param>
    private void DisplayMeetingError(MeetingRow selectedMeeting)
    {
        var meetingError = _tmDb.GetMeetingError(selectedMeeting.MeetingId);

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


    /// <summary>Extracts a named string property value from a <see cref="JsonElement"/>.</summary>
    /// <remarks>Returns <see cref="string.Empty"/> if the property is absent or its value is null.</remarks>
    /// <param name="propertyName">The name of the property to extract.</param>
    /// <param name="meetingDetail">The JSON element to extract the property from.</param>
    /// <returns>The property's string value, or <see cref="string.Empty"/> if absent or null.</returns>
    private static string GetStringProperty(string propertyName, JsonElement? meetingDetail)
    {
        return meetingDetail.Value.TryGetProperty(propertyName, out var elem)
            ? (elem.GetString() ?? string.Empty)
            : string.Empty;
    }

    /// <summary>Sanitizes a string value, replacing null literals and empty values with '---'.</summary>
    /// <remarks>Uses a case-insensitive regex to detect and replace literal null tokens within the string.</remarks>
    /// <param name="value">The string value to sanitize.</param>
    /// <returns>The sanitized string value, or '---' if null, whitespace, or only null tokens.</returns>
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