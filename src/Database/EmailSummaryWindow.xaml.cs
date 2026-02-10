using System.Windows;

namespace TingenTransmorger.Database;

/// <summary>
/// Interaction logic for EmailSummaryWindow.xaml
/// </summary>
public partial class EmailSummaryWindow : Window
{
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures;
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries;

    public EmailSummaryWindow(
        List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> emailFailures,
        List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> emailDeliveries)
    {
        InitializeComponent();

        SetEmailData(emailFailures, emailDeliveries);
    }

    public void SetEmailData(
        List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> emailFailures,
        List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> emailDeliveries)
    {
        _emailFailures = emailFailures;
        _emailDeliveries = emailDeliveries;

        // Combine both lists into a unified email history
        var combinedEmails = new List<EmailHistoryRow>();

        // Add Email Failures
        foreach (var failure in emailFailures)
        {
            var formattedStartTime = FormatStartTime(failure.ScheduledStartTime);
            
            combinedEmails.Add(new EmailHistoryRow
            {
                IsFailure = true,
                Sent = "---", // Show --- for failures
                ScheduleStartTime = formattedStartTime,
                Status = "Failed",
                MessageType = "Email",
                ErrorDetails = FormatErrorDetails(failure.ErrorMessage),
                EmailAddress = failure.EmailAddress ?? string.Empty,
                Type = "Failure",
                SortTimestamp = ParseTimestamp(failure.ScheduledStartTime)
            });
        }

        // Add Email Deliveries
        foreach (var delivery in emailDeliveries)
        {
            // Combine date and time for successful deliveries
            var sent = CombineDateAndTime(delivery.DateSent, delivery.TimeSent);
            var formattedSent = string.IsNullOrWhiteSpace(sent) ? "---" : sent;
            
            combinedEmails.Add(new EmailHistoryRow
            {
                IsFailure = false,
                Sent = formattedSent,
                ScheduleStartTime = "---", // Show --- for successful deliveries
                Status = delivery.DeliveryStatus ?? string.Empty,
                MessageType = delivery.MessageType ?? string.Empty,
                ErrorDetails = FormatErrorDetails(delivery.ErrorMessage),
                EmailAddress = delivery.EmailAddress ?? string.Empty,
                Type = "Delivery",
                SortTimestamp = ParseTimestamp(sent)
            });
        }

        // Sort by most recent first
        var sortedEmails = combinedEmails
            .OrderByDescending(m => m.SortTimestamp)
            .ToList();

        dgEmails.ItemsSource = sortedEmails;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private string CombineDateAndTime(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date))
            return string.Empty;
        
        if (string.IsNullOrWhiteSpace(time))
            return date;
        
        return $"{date} {time}";
    }

    private DateTime ParseTimestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return DateTime.MinValue;

        // Try to parse the timestamp, return MinValue if parsing fails
        if (DateTime.TryParse(timestamp, out var result))
            return result;

        return DateTime.MinValue;
    }

    /// <summary>
    /// Formats the start time to MM/DD/YY HH:MM AM/PM format, or returns "---" if empty.
    /// </summary>
    private string FormatStartTime(string? startTime)
    {
        if (string.IsNullOrWhiteSpace(startTime))
            return "---";

        // Try to parse the datetime
        if (DateTime.TryParse(startTime, out var dt))
        {
            // Format as MM/DD/YY HH:MM AM/PM
            return dt.ToString("MM/dd/yy hh:mm tt");
        }

        // If parsing fails, return the original value or ---
        return string.IsNullOrWhiteSpace(startTime) ? "---" : startTime;
    }

    /// <summary>
    /// Formats error details, showing "---" if empty or if content is "{}".
    /// </summary>
    private string FormatErrorDetails(string? errorDetails)
    {
        if (string.IsNullOrWhiteSpace(errorDetails))
            return "---";

        // Check if content is just "{}"
        if (errorDetails.Trim() == "{}")
            return "---";

        return errorDetails;
    }
}

/// <summary>
/// Represents a row in the combined email history grid.
/// </summary>
public class EmailHistoryRow
{
    public bool IsFailure { get; set; }
    public string Sent { get; set; } = string.Empty;
    public string ScheduleStartTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime SortTimestamp { get; set; }
}
