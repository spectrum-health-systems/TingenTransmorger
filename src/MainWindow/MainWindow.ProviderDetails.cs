// 260225_code
// 260225_documentation

using System.Text.Json;
using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.ProviderDetails partial class contains logic related to displaying provider details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary> Currently selected provider name.</summary>
    private string _currentProviderName = string.Empty;

    /// <summary>Currently selected provider ID.</summary>
    private string _currentProviderId = string.Empty;

    /// <summary>Displays provider details in the UI.</summary>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        _currentProviderName = providerName;
        _currentProviderId   = providerId;

        // Get provider details from database
        JsonElement? providerDetails = TmDb.GetProviderDetails(providerName);

        if (providerDetails == null)
        {
            StopApp($"Critical error! [MW8001]");
        }

        SetProviderDetailUi(providerName, providerId);

        DisplayProviderMeetingResults();

        /* There isn't a way to easily match providers to their email addresses, so we aren't going to do that for now.
         * Eventually we should, and this is (probably) where that logic should go.
         */


        //////    // Display meetings for this provider
        //////    var meetingRows = new List<PatientMeetingRow>();
        //////    if (providerDetails.Value.TryGetProperty("Meetings", out var meetingsElement))
        //////    {
        //////        if (meetingsElement.ValueKind == JsonValueKind.Array)
        //////        {
        //////            foreach (var meetingIdElement in meetingsElement.EnumerateArray())
        //////            {
        //////                var meetingId = meetingIdElement.GetString();
        //////                if (string.IsNullOrWhiteSpace(meetingId))
        //////                    continue;

        //////                // Get meeting details from MeetingDetail
        //////                var meetingDetail = TmDb.GetMeetingDetail(meetingId);
        //////                if (meetingDetail == null)
        //////                    continue;

        //////                // Get meeting information
        //////                var scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
        //////                    ? startElem.GetString() : string.Empty;
        //////                var status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
        //////                    ? statusElem.GetString() : string.Empty;
        //////                var duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
        //////                    ? durationElem.GetString() : string.Empty;

        //////                // For providers, we don't have patient-specific arrival/drop times
        //////                string ReplaceNull(string value)
        //////                {
        //////                    if (string.IsNullOrWhiteSpace(value))
        //////                        return "---";

        //////                    var result = System.Text.RegularExpressions.Regex.Replace(
        //////                        value,
        //////                        @"\bnull\b",
        //////                        "<<NULL>>",
        //////                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        //////                    if (result.Contains("<<NULL>>"))
        //////                    {
        //////                        var cleanedResult = result.Replace("<<NULL>>", "").Trim().Trim(',').Trim(';').Trim();
        //////                        if (string.IsNullOrWhiteSpace(cleanedResult))
        //////                        {
        //////                            return "---";
        //////                        }
        //////                        result = result.Replace("<<NULL>>", "---");
        //////                    }

        //////                    return string.IsNullOrWhiteSpace(result) ? "---" : result;
        //////                }

        //////                // Check if meeting has an error
        //////                var hasError = TmDb.HasMeetingError(meetingId);

        //////                // Check status flags
        //////                var statusLower = status?.ToLower() ?? string.Empty;
        //////                var isCancelled = statusLower.Contains("cancel");
        //////                var isCompleted = statusLower.Contains("complete");

        //////                meetingRows.Add(new PatientMeetingRow
        //////                {
        //////                    MeetingId = meetingId,
        //////                    Start = ReplaceNull(scheduledStart ?? string.Empty),
        //////                    Arrived = "N/A",  // Not applicable for provider view
        //////                    Dropped = "N/A",  // Not applicable for provider view
        //////                    Duration = ReplaceNull(duration ?? string.Empty),
        //////                    Status = ReplaceNull(status ?? string.Empty),
        //////                    HasError = hasError,
        //////                    IsCancelled = isCancelled,
        //////                    IsCompleted = isCompleted
        //////                });
        //////            }
        //////        }
        //////    }

        //////    // Sort meetings by ScheduledStart descending (most recent first)
        //////    meetingRows = meetingRows
        //////        .OrderByDescending(m => m.Start)
        //////        .ToList();

        //////    // Count meetings by status
        //////    var totalCount = meetingRows.Count;
        //////    var completedCount = meetingRows.Count(m => m.IsCompleted);
        //////    var cancelledCount = meetingRows.Count(m => m.IsCancelled);

        //////    var inProgressCount = 0;
        //////    var expiredCount = 0;
        //////    var scheduledCount = 0;

        //////    foreach (var meeting in meetingRows)
        //////    {
        //////        var statusLower = meeting.Status?.ToLower() ?? string.Empty;

        //////        if (meeting.IsCompleted || meeting.IsCancelled)
        //////            continue;

        //////        if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
        //////            inProgressCount++;
        //////        else if (statusLower.Contains("expired"))
        //////            expiredCount++;
        //////        else if (statusLower.Contains("scheduled"))
        //////            scheduledCount++;
        //////    }

        //////    // Update the header with the detailed count
        //////    txbkTotalMeetingsValue.Text = $"{totalCount} MEETINGS";
        //////    txbkCompletedMeetingsValue.Text = $"{completedCount} Completed";
        //////    txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        //////    txbkMeetingsExpiredValue.Text = $"{expiredCount} Expired";
        //////    txbkMeetingsCancelledValue.Text = $"{cancelledCount} Cancelled";
        //////    txbkMeetingsScheduledValue.Text = $"{scheduledCount} Scheduled";

        //////    // Bind to DataGrid
        //////    dgrdMeetingResults.ItemsSource = meetingRows;

        //////    // Show meetings section if there are meetings
        //////    if (meetingRows.Count > 0)
        //////    {
        //////        spnlMeetingComponents.Visibility = Visibility.Visible;
        //////    }
        //////    else
        //////    {
        //////        spnlMeetingComponents.Visibility = Visibility.Collapsed;
        //////    }

        //////    // Hide meeting details until a meeting is selected
        //////    spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
        //////}
    }
}
