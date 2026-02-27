// 260227_code
// 260227_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.ProviderDetails partial class contains logic related to displaying provider details .
 */
public partial class MainWindow : Window
{
    //TODO: This should be moved to a common area.
    /// <summary>Returns the string value of a named property on a <see cref="JsonElement"/>, or <see cref="string.Empty"/> if the property is absent or null.</summary>
    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop)
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>Displays provider details in the UI.</summary>
    /// <param name="providerName">The provider name</param>
    /// <param name="providerId">The provider ID.</param>
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

    /// <summary>Display meeting statistics and details for the specified provider. </summary>
    /// <param name="providerName">The name of the provider.</param>
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

    /// <summary>Build a list of meeting details.</summary>
    /// <remarks>This will go through all of the meeting IDs for the provider, and add valid details to the list.</remarks>
    /// <param name="meetingRows">The list of meeting details.</param>
    /// <param name="providerDetails">Provider details component of the database, should include a 'Meetings' property.</param>
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

    /// <summary>Create a MeetingRow object containing details of a meeting</summary>
    /// <param name="meetingId">The meeting ID.</param>
    /// <param name="meetingDetail">The meeting details</returns>
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