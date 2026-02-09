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
            combinedMessages.Add(new MessageHistoryRow
            {
                MessageCategory = "Failure",
                IsFailure = true,
                PhoneNumber = failure.PhoneNumber ?? string.Empty,
                Date = ExtractDate(failure.ScheduledStartTime),
                Time = ExtractTime(failure.ScheduledStartTime),
                Status = "Failed",
                MessageType = "SMS",
                ErrorMessage = failure.ErrorMessage ?? string.Empty,
                SortTimestamp = ParseTimestamp(failure.ScheduledStartTime)
            });
        }

        // Add Message Deliveries
        foreach (var delivery in messageDeliveries)
        {
            combinedMessages.Add(new MessageHistoryRow
            {
                MessageCategory = "Delivery",
                IsFailure = false,
                PhoneNumber = delivery.PhoneNumber ?? string.Empty,
                Date = delivery.DateSent ?? string.Empty,
                Time = delivery.TimeSent ?? string.Empty,
                Status = delivery.DeliveryStatus ?? string.Empty,
                MessageType = delivery.MessageType ?? string.Empty,
                ErrorMessage = delivery.ErrorMessage ?? string.Empty,
                SortTimestamp = ParseTimestamp($"{delivery.DateSent} {delivery.TimeSent}")
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

    private void btnDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        // Get the parent window's TransMorgDb
        if (Owner is MainWindow mainWindow && mainWindow.TransMorgDb != null)
        {
            var allProperties = mainWindow.TransMorgDb.ListAllRootProperties();
            var structureInfo = mainWindow.TransMorgDb.GetDatabaseStructureDiagnostic();
            var smsInfo = mainWindow.TransMorgDb.GetFirstSmsFailureDiagnostic();
            var searchInfo = mainWindow.TransMorgDb.SearchForSmsFailureRecords();
            
            var message = $"{allProperties}\n\n{'='.ToString().PadRight(80, '=')}\n\n{structureInfo}\n\n{'='.ToString().PadRight(80, '=')}\n\n{smsInfo}\n\n{'='.ToString().PadRight(80, '=')}\n\n{searchInfo}";
            
            var diagnosticWindow = new DiagnosticWindow();
            diagnosticWindow.SetDiagnosticText(message);
            diagnosticWindow.Owner = this;
            diagnosticWindow.ShowDialog();
        }
        else
        {
            MessageBox.Show("Database not available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string ExtractDate(string scheduledStartTime)
    {
        if (string.IsNullOrWhiteSpace(scheduledStartTime))
            return string.Empty;

        // Assuming format like "MM/DD/YYYY HH:MM:SS" or similar
        var parts = scheduledStartTime.Split(' ');
        return parts.Length > 0 ? parts[0] : scheduledStartTime;
    }

    private string ExtractTime(string scheduledStartTime)
    {
        if (string.IsNullOrWhiteSpace(scheduledStartTime))
            return string.Empty;

        // Assuming format like "MM/DD/YYYY HH:MM:SS" or similar
        var parts = scheduledStartTime.Split(' ');
        return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;
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
}

/// <summary>
/// Represents a row in the combined message history grid.
/// </summary>
public class MessageHistoryRow
{
    public string MessageCategory { get; set; } = string.Empty;
    public bool IsFailure { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime SortTimestamp { get; set; }
}


