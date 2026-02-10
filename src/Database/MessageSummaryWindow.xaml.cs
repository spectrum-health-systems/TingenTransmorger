using System.Windows;

namespace TingenTransmorger.Database;

/// <summary>
/// Interaction logic for MessageSummaryWindow.xaml
/// </summary>
public partial class MessageSummaryWindow : Window
{
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures;
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _messageDeliveries;

    public MessageSummaryWindow(
        List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> smsFailures,
        List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> messageDeliveries)
    {
        InitializeComponent();

        SetMessageData(smsFailures, messageDeliveries);
    }

    public void SetMessageData(
        List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> smsFailures,
        List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> messageDeliveries)
    {
        _smsFailures = smsFailures;
        _messageDeliveries = messageDeliveries;

        // Combine both lists into a unified message history
        var combinedMessages = new List<MessageHistoryRow>();

        // Add SMS Failures
        foreach (var failure in smsFailures)
        {
            var formattedStartTime = FormatStartTime(failure.ScheduledStartTime);
            
            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = true,
                Sent = "---", // Show --- for failures
                ScheduleStartTime = formattedStartTime,
                Status = "Failed",
                MessageType = "SMS",
                ErrorDetails = FormatErrorDetails(failure.ErrorMessage),
                PhoneNumber = failure.PhoneNumber ?? string.Empty,
                Type = "Failure",
                SortTimestamp = ParseTimestamp(failure.ScheduledStartTime)
            });
        }

        // Add Message Deliveries
        foreach (var delivery in messageDeliveries)
        {
            // Combine date and time for successful deliveries
            var sent = CombineDateAndTime(delivery.DateSent, delivery.TimeSent);
            var formattedSent = string.IsNullOrWhiteSpace(sent) ? "---" : sent;
            
            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = false,
                Sent = formattedSent,
                ScheduleStartTime = "---", // Show --- for successful deliveries
                Status = delivery.DeliveryStatus ?? string.Empty,
                MessageType = delivery.MessageType ?? string.Empty,
                ErrorDetails = FormatErrorDetails(delivery.ErrorMessage),
                PhoneNumber = delivery.PhoneNumber ?? string.Empty,
                Type = "Delivery",
                SortTimestamp = ParseTimestamp(sent)
            });
        }

        // Sort by most recent first
        var sortedMessages = combinedMessages
            .OrderByDescending(m => m.SortTimestamp)
            .ToList();

        dgMessages.ItemsSource = sortedMessages;
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
/// Represents a row in the combined message history grid.
/// </summary>
public class MessageHistoryRow
{
    public bool IsFailure { get; set; }
    public string Sent { get; set; } = string.Empty;
    public string ScheduleStartTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime SortTimestamp { get; set; }
}


