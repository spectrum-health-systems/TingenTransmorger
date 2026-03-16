// 260227_code
// 260311_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.ProviderDetails partial class contains logic related to displaying provider details .
 */
public partial class MainWindow : Window
{
    //TODO: This should be moved to a common area.
    /// <summary>Extracts a named string property value from a <see cref="JsonElement"/>.</summary>
    /// <remarks>Returns <see cref="string.Empty"/> if the property is absent or its value is null.</remarks>
    /// <param name="element">The JSON element to extract the property from.</param>
    /// <param name="propertyName">The name of the property to extract.</param>
    /// <returns>The property's string value, or <see cref="string.Empty"/> if absent or null.</returns>
    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop)
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>Loads and displays all provider detail sections in the UI for the specified provider.</summary>
    /// <remarks>Calls <see cref="StopApp"/> if no provider record is found in the database.</remarks>
    /// <param name="providerName">The full name of the provider to display.</param>
    /// <param name="providerId">The unique identifier of the provider to display.</param>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        // Get provider details from database
        JsonElement? providerDetails = _tmDb.GetProviderDetails(providerName);

        if (providerDetails == null)
        {
            StopApp($"Critical error: Provider details not found. [MW8001]");
        }

        SetProviderDetailUi(providerName, providerId);
        DisplayProviderMeetingResults(providerName);
    }

    /// <summary>Builds and displays the meeting list and status summary for the specified provider.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Sorts meetings by scheduled start date descending before binding to the data grid.</item>
    /// <item>Computes completed, cancelled, in-progress, expired, and scheduled meeting counts.</item>
    /// </list>
    /// </remarks>
    /// <param name="providerName">The full name of the provider whose meetings will be displayed.</param>
    private void DisplayProviderMeetingResults(string providerName)
    {
        var meetingList     = new List<MeetingRow>();
        var providerDetails = _tmDb.GetProviderDetails(providerName);

        BuildMeetingList(meetingList, providerDetails);

        meetingList = [.. meetingList.OrderByDescending(m => m.ScheduledStart)];

        var totalCount      = meetingList.Count;
        var completedCount  = meetingList.Count(m => m.IsCompleted);
        var cancelledCount  = meetingList.Count(m => m.IsCancelled);
        var inProgressCount = 0;
        var expiredCount    = 0;
        var scheduledCount  = 0;

        foreach (var meeting in meetingList)
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

        dgrdMeetingList.ItemsSource = meetingList;

        spnlMeetingDetail.Visibility = meetingList.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        spnlMeetingDetail.Visibility = Visibility.Collapsed;
    }

    /// <summary>Populates a meeting row list from the provider's meeting ID array in the JSON record.</summary>
    /// <remarks>Skips null or whitespace meeting IDs and any IDs with no corresponding detail record.</remarks>
    /// <param name="meetingRows">The list to populate with resolved meeting row data.</param>
    /// <param name="providerDetails">The JSON element containing the provider's meeting ID array.</param>
    private void BuildMeetingList(List<MeetingRow> meetingRows, JsonElement? providerDetails)
    {
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

                    var meetingDetail  = _tmDb.GetMeetingDetail(meetingId);

                    if (meetingDetail == null)
                    {
                        continue;
                    }

                    var row = GetMeetingRowDetails(meetingId, meetingDetail);

                    if (row != null)
                    {
                        meetingRows.Add(row);
                    }
                }
            }
        }
    }

    /// <summary>Builds a <see cref="MeetingRow"/> from a meeting ID and its JSON detail record.</summary>
    /// <remarks>Returns <see langword="null"/> if <paramref name="meetingDetail"/> is null.</remarks>
    /// <param name="meetingId">The meeting ID to assign to the row.</param>
    /// <param name="meetingDetail">The JSON element containing the meeting's detail fields.</param>
    /// <returns>A populated <see cref="MeetingRow"/>, or <see langword="null"/> if the detail record is absent.</returns>
    private MeetingRow? GetMeetingRowDetails(string meetingId, JsonElement? meetingDetail)
    {
        if (meetingDetail == null)
        {
            return null;
        }

        var statusLower = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "Status"))?.ToLower() ?? string.Empty;

        return new MeetingRow
        {
            MeetingId      = meetingId,
            ScheduledStart = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ScheduledStart")),
            ActualStart    = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ActualStart")),
            ScheduledEnd   = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ScheduledEnd")),
            ActualEnd      = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ActualEnd")),
            Duration       = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "Duration")),
            Status         = statusLower,
            HasError       = _tmDb.HasMeetingError(meetingId),
            IsCancelled    = statusLower.Contains("cancel"),
            IsCompleted    = statusLower.Contains("complete")
        };
    }
}