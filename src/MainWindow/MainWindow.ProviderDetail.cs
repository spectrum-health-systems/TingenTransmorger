// 260226_code
// 260226_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.ProviderDetails partial class contains logic related to displaying provider details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary>Returns the string value of a named property on a <see cref="JsonElement"/>, or <see cref="string.Empty"/> if the property is absent or null.</summary>
    private static string GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? string.Empty : string.Empty;

    /// <summary>Displays provider details in the UI.</summary>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        // Get provider details from database
        JsonElement? providerDetails = TmDb.GetProviderDetails(providerName);

        if (providerDetails == null)
        {
            StopApp($"Critical error! [MW8001]");
        }

        SetProviderDetailUi(providerName, providerId);
        /* There isn't a way to easily match providers to their email addresses, so we aren't going to do that for now.
         * Eventually we should, and this is (probably) where that logic should go. For now I've put the code I was
         * working on in .github/Development/ProviderEmailLogic.md.
         */
        DisplayProviderMeetingResults(providerName);
    }

    private void DisplayProviderMeetingResults(string providerName)
    {
        var meetingRows = new List<MeetingRow>();

        var providerDetails = TmDb.GetProviderDetails(providerName);

        /* Verify that provider details exist and contain a "Meetings" property before attempting to enumerate meetings.
         * TryGetProperty returns false if the property is missing, preventing KeyNotFoundException on the JsonElement.
         */
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

    private MeetingRow? GetMeetingRowDetails(string meetingId, JsonElement? meetingDetail)
    {
        if (meetingDetail == null)
        {
            return null;
        }

        var statusLower = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "Status"))?.ToLower() ?? string.Empty;

        return new MeetingRow
        {
            MeetingId     = meetingId,
            ScheduledStart = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ScheduledStart")),
            ActualStart   = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ActualStart")),
            ScheduledEnd  = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ScheduledEnd")),
            ActualEnd     = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "ActualEnd")),
            Duration      = ReplaceNullValues(GetStringProperty(meetingDetail.Value, "Duration")),
            Status        = statusLower,
            HasError      = TmDb.HasMeetingError(meetingId),
            IsCancelled   = statusLower.Contains("cancel"),
            IsCompleted   = statusLower.Contains("complete")
        };
    }
}
