using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TingenTransmorger.Database;

/// <summary>
/// Message type for the MessageHistoryWindow
/// </summary>
public enum MessageHistoryType
{
    SMS,
    Email
}

/// <summary>
/// Interaction logic for MessageHistoryWindow.xaml
/// </summary>
public partial class MessageHistoryWindow : Window
{
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures;
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _messageDeliveries;
    private MessageHistoryType _messageType;

    // Constructor for SMS messages
    public MessageHistoryWindow(
        List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> smsFailures,
        List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> messageDeliveries)
    {
        InitializeComponent();
        _messageType = MessageHistoryType.SMS;
        ConfigureForMessageType();
        SetMessageData(smsFailures, messageDeliveries);
    }

    // Constructor for Email messages
    public MessageHistoryWindow(
        List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> emailFailures,
        List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> emailDeliveries,
        MessageHistoryType messageType)
    {
        InitializeComponent();
        _messageType = messageType;
        ConfigureForMessageType();
        SetEmailData(emailFailures, emailDeliveries);
    }

    private void btnCopyAllSuccessMessageHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Take current items (respecting sorting/filtering) and filter successes
            var items = dgMessages.Items.Cast<object>().Where(i => i != null).ToList();
            var successes = new List<MessageHistoryRow>();

            foreach (var item in items)
            {
                if (item is MessageHistoryRow mr)
                {
                    if (!string.Equals(mr.Type, "Failure", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(mr.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        // treat as success unless explicitly marked failure
                        successes.Add(mr);
                    }
                }
                else
                {
                    // try reflection fallback: look for Status or Type property
                    var t = item.GetType();
                    var statusProp = t.GetProperty("Status");
                    var typeProp = t.GetProperty("Type");
                    var statusVal = statusProp?.GetValue(item)?.ToString() ?? string.Empty;
                    var typeVal = typeProp?.GetValue(item)?.ToString() ?? string.Empty;
                    if (!string.Equals(typeVal, "Failure", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(statusVal, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        successes.Add(new MessageHistoryRow
                        {
                            Sent = t.GetProperty("Sent")?.GetValue(item)?.ToString() ?? string.Empty,
                            ScheduleStartTime = t.GetProperty("ScheduleStartTime")?.GetValue(item)?.ToString() ?? string.Empty,
                            Status = statusVal,
                            MessageType = t.GetProperty("MessageType")?.GetValue(item)?.ToString() ?? string.Empty,
                            ErrorDetails = t.GetProperty("ErrorDetails")?.GetValue(item)?.ToString() ?? string.Empty,
                            PhoneNumber = t.GetProperty("PhoneNumber")?.GetValue(item)?.ToString() ?? string.Empty,
                            Type = typeVal,
                        });
                    }
                }
            }

            if (successes.Count == 0)
            {
                MessageBox.Show(this, "No success rows found to copy.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Format successes similar to other copy methods
            var contactHeader = _messageType == MessageHistoryType.SMS ? "Phone Number" : "Email Address";
            var headerNames = new[] { "Sent / Start Time", "Status", "Message Type", "Error/Details", contactHeader, "Type" };
            var rowList = successes.Select(r => new[] {
                r.SentOrStartTime ?? string.Empty,
                r.Status ?? string.Empty,
                r.MessageType ?? string.Empty,
                r.ErrorDetails ?? string.Empty,
                (_messageType == MessageHistoryType.SMS ? r.PhoneNumber : r.EmailAddress) ?? string.Empty,
                r.Type ?? string.Empty
            }).ToList();

            var colCaps = new[] { 30, 20, 30, 120, 20, 15 };
            var widths = new int[headerNames.Length];
            for (int c = 0; c < headerNames.Length; c++)
            {
                int max = headerNames[c].Length;
                foreach (var r in rowList)
                    max = Math.Max(max, (r[c]?.Length) ?? 0);
                widths[c] = Math.Min(max, colCaps[c]);
            }

            static string Truncate(string s, int w)
            {
                if (s == null)
                    return string.Empty;
                if (s.Length <= w)
                    return s;
                if (w <= 3)
                    return s.Substring(0, w);
                return s.Substring(0, w - 3) + "...";
            }

            var sb = new StringBuilder();
            for (int c = 0; c < headerNames.Length; c++)
            {
                sb.Append(headerNames[c].PadRight(widths[c]));
                if (c < headerNames.Length - 1)
                    sb.Append("  ");
            }
            sb.AppendLine();

            foreach (var r in rowList)
            {
                for (int c = 0; c < r.Length; c++)
                {
                    var cell = Escape(Truncate(r[c] ?? string.Empty, widths[c]));
                    sb.Append(cell.PadRight(widths[c]));
                    if (c < r.Length - 1)
                        sb.Append("  ");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Success message history rows copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy message history: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnCopyAllErrorMessageHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Take current items (respecting sorting/filtering) and filter failures
            var items = dgMessages.Items.Cast<object>().Where(i => i != null).ToList();
            var failures = new List<MessageHistoryRow>();

            foreach (var item in items)
            {
                if (item is MessageHistoryRow mr)
                {
                    if (string.Equals(mr.Type, "Failure", StringComparison.OrdinalIgnoreCase) || string.Equals(mr.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                        failures.Add(mr);
                }
                else
                {
                    // try reflection fallback: look for Status or Type property
                    var t = item.GetType();
                    var statusProp = t.GetProperty("Status");
                    var typeProp = t.GetProperty("Type");
                    var statusVal = statusProp?.GetValue(item)?.ToString() ?? string.Empty;
                    var typeVal = typeProp?.GetValue(item)?.ToString() ?? string.Empty;
                    if (string.Equals(typeVal, "Failure", StringComparison.OrdinalIgnoreCase) || string.Equals(statusVal, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        // map reflected values into MessageHistoryRow minimally
                        failures.Add(new MessageHistoryRow
                        {
                            Sent = t.GetProperty("Sent")?.GetValue(item)?.ToString() ?? string.Empty,
                            ScheduleStartTime = t.GetProperty("ScheduleStartTime")?.GetValue(item)?.ToString() ?? string.Empty,
                            Status = statusVal,
                            MessageType = t.GetProperty("MessageType")?.GetValue(item)?.ToString() ?? string.Empty,
                            ErrorDetails = t.GetProperty("ErrorDetails")?.GetValue(item)?.ToString() ?? string.Empty,
                            PhoneNumber = t.GetProperty("PhoneNumber")?.GetValue(item)?.ToString() ?? string.Empty,
                            Type = typeVal,
                        });
                    }
                }
            }

            if (failures.Count == 0)
            {
                MessageBox.Show(this, "No failure rows found to copy.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Format failures similar to other copy methods
            var contactHeader = _messageType == MessageHistoryType.SMS ? "Phone Number" : "Email Address";
            var headerNames = new[] { "Sent / Start Time", "Status", "Message Type", "Error/Details", contactHeader, "Type" };
            var rowList = failures.Select(r => new[] {
                r.SentOrStartTime ?? string.Empty,
                r.Status ?? string.Empty,
                r.MessageType ?? string.Empty,
                r.ErrorDetails ?? string.Empty,
                (_messageType == MessageHistoryType.SMS ? r.PhoneNumber : r.EmailAddress) ?? string.Empty,
                r.Type ?? string.Empty
            }).ToList();

            var colCaps = new[] { 30, 20, 30, 120, 20, 15 };
            var widths = new int[headerNames.Length];
            for (int c = 0; c < headerNames.Length; c++)
            {
                int max = headerNames[c].Length;
                foreach (var r in rowList)
                    max = Math.Max(max, (r[c]?.Length) ?? 0);
                widths[c] = Math.Min(max, colCaps[c]);
            }

            static string Truncate(string s, int w)
            {
                if (s == null)
                    return string.Empty;
                if (s.Length <= w)
                    return s;
                if (w <= 3)
                    return s.Substring(0, w);
                return s.Substring(0, w - 3) + "...";
            }

            var sb = new StringBuilder();
            for (int c = 0; c < headerNames.Length; c++)
            {
                sb.Append(headerNames[c].PadRight(widths[c]));
                if (c < headerNames.Length - 1)
                    sb.Append("  ");
            }
            sb.AppendLine();

            foreach (var r in rowList)
            {
                for (int c = 0; c < r.Length; c++)
                {
                    var cell = Escape(Truncate(r[c] ?? string.Empty, widths[c]));
                    sb.Append(cell.PadRight(widths[c]));
                    if (c < r.Length - 1)
                        sb.Append("  ");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Failure message history rows copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy message history: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnCopyTopTenMessageHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use the DataGrid's current item ordering (dgMessages.Items) so we respect user sorting/filtering
            var items = dgMessages.Items.Cast<object>().Where(i => i != null).Take(10).ToList();

            var contactHeader = _messageType == MessageHistoryType.SMS ? "Phone Number" : "Email Address";
            var headerNames = new[] { "Sent / Start Time", "Status", "Message Type", "Error/Details", contactHeader, "Type" };
            var rowList = new List<string[]>();

            foreach (var item in items)
            {
                if (item is MessageHistoryRow mr)
                {
                    rowList.Add(new[] {
                        mr.SentOrStartTime ?? string.Empty,
                        mr.Status ?? string.Empty,
                        mr.MessageType ?? string.Empty,
                        mr.ErrorDetails ?? string.Empty,
                        (_messageType == MessageHistoryType.SMS ? mr.PhoneNumber : mr.EmailAddress) ?? string.Empty,
                        mr.Type ?? string.Empty
                    });
                }
                else
                {
                    // Fallback to reflecting bound properties for this item
                    var values = new List<string>();
                    foreach (var col in dgMessages.Columns)
                    {
                        if (col is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding && !string.IsNullOrEmpty(binding.Path?.Path))
                        {
                            var prop = item.GetType().GetProperty(binding.Path.Path);
                            var val = prop?.GetValue(item)?.ToString() ?? string.Empty;
                            values.Add(val);
                        }
                        else
                        {
                            values.Add(item.ToString() ?? string.Empty);
                        }
                    }
                    rowList.Add(values.ToArray());
                }
            }

            if (rowList.Count == 0)
            {
                Clipboard.SetText(string.Empty);
                MessageBox.Show(this, "No message history rows to copy.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Column caps to avoid extremely wide output
            var colCaps = new[] { 30, 20, 30, 120, 20, 15 };
            var widths = new int[headerNames.Length];
            for (int c = 0; c < headerNames.Length; c++)
            {
                int max = headerNames[c].Length;
                foreach (var r in rowList)
                    max = Math.Max(max, (r[c]?.Length) ?? 0);
                widths[c] = Math.Min(max, colCaps[c]);
            }

            static string Truncate(string s, int w)
            {
                if (s == null)
                    return string.Empty;
                if (s.Length <= w)
                    return s;
                if (w <= 3)
                    return s.Substring(0, w);
                return s.Substring(0, w - 3) + "...";
            }

            var sb = new StringBuilder();
            for (int c = 0; c < headerNames.Length; c++)
            {
                sb.Append(headerNames[c].PadRight(widths[c]));
                if (c < headerNames.Length - 1)
                    sb.Append("  ");
            }
            sb.AppendLine();

            foreach (var r in rowList)
            {
                for (int c = 0; c < r.Length; c++)
                {
                    var cell = Escape(Truncate(r[c] ?? string.Empty, widths[c]));
                    sb.Append(cell.PadRight(widths[c]));
                    if (c < r.Length - 1)
                        sb.Append("  ");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Top 10 message history rows copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy message history: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ConfigureForMessageType()
    {
        // Update window title
        Title = _messageType == MessageHistoryType.SMS ? "SMS Message History" : "Email Message History";

        // Update label content
        lblMessageHistoryTitle.Content = _messageType == MessageHistoryType.SMS ? "Message History - Phone" : "Message History - Email";

        // Update the contact column header and binding
        var contactColumn = dgMessages.Columns[4] as DataGridTextColumn; // Phone Number / Email Address column
        if (contactColumn != null)
        {
            contactColumn.Header = _messageType == MessageHistoryType.SMS ? "Phone Number" : "Email Address";
            contactColumn.Binding = new System.Windows.Data.Binding(_messageType == MessageHistoryType.SMS ? "PhoneNumber" : "EmailAddress");
        }
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

            // Detect opt-out error - check for "is opted out" or general "opt out" phrases
            var isOptedOut = !string.IsNullOrWhiteSpace(failure.ErrorMessage)
                && (failure.ErrorMessage.Contains("is opted out", StringComparison.OrdinalIgnoreCase)
                    || failure.ErrorMessage.Contains("opted out", StringComparison.OrdinalIgnoreCase)
                    || failure.ErrorMessage.Contains("opt-out", StringComparison.OrdinalIgnoreCase));

            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = true,
                // For failures Sent is always ---
                Sent = "---",
                // Format ScheduleStartTime using MM/DD/YY HH:MM AM/PM
                ScheduleStartTime = FormatStartTime(formattedStartTime),
                Status = "Failed",
                MessageType = "SMS",
                ErrorDetails = isOptedOut ? "Opted out" : FormatErrorDetails(failure.ErrorMessage),
                PhoneNumber = failure.PhoneNumber ?? string.Empty,
                EmailAddress = string.Empty,
                Type = "Failure",
                SortTimestamp = ParseTimestamp(failure.ScheduledStartTime)
            });
        }

        // Add Message Deliveries
        foreach (var delivery in messageDeliveries)
        {
            // Combine date and time for successful deliveries
            var sent = CombineDateAndTime(delivery.DateSent, delivery.TimeSent);
            // Format Sent using MM/DD/YY HH:MM AM/PM (FormatStartTime handles empty/invalid values)
            var formattedSent = FormatStartTime(sent);

            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = false,
                Sent = formattedSent,
                ScheduleStartTime = "---", // Show --- for successful deliveries
                Status = delivery.DeliveryStatus ?? string.Empty,
                MessageType = delivery.MessageType ?? string.Empty,
                ErrorDetails = FormatErrorDetails(delivery.ErrorMessage),
                PhoneNumber = delivery.PhoneNumber ?? string.Empty,
                EmailAddress = string.Empty,
                Type = "Delivery",
                SortTimestamp = ParseTimestamp(sent)
            });
        }

        // Sort by most recent first
        var sortedMessages = combinedMessages
            .OrderByDescending(m => m.SortTimestamp)
            .ToList();

        dgMessages.ItemsSource = sortedMessages;
        UpdateSummary(sortedMessages);
    }

    public void SetEmailData(
        List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> emailFailures,
        List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> emailDeliveries)
    {
        _smsFailures = null;
        _messageDeliveries = null;

        // Combine both lists into a unified message history
        var combinedMessages = new List<MessageHistoryRow>();

        // Add Email Failures
        foreach (var failure in emailFailures)
        {
            var formattedStartTime = FormatStartTime(failure.ScheduledStartTime);

            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = true,
                Sent = "---",
                ScheduleStartTime = FormatStartTime(formattedStartTime),
                Status = "Failed",
                MessageType = "Email",
                ErrorDetails = FormatErrorDetails(failure.ErrorMessage),
                PhoneNumber = string.Empty,
                EmailAddress = failure.EmailAddress ?? string.Empty,
                Type = "Failure",
                SortTimestamp = ParseTimestamp(failure.ScheduledStartTime)
            });
        }

        // Add Email Deliveries
        foreach (var delivery in emailDeliveries)
        {
            var sent = CombineDateAndTime(delivery.DateSent, delivery.TimeSent);
            var formattedSent = FormatStartTime(sent);

            combinedMessages.Add(new MessageHistoryRow
            {
                IsFailure = false,
                Sent = formattedSent,
                ScheduleStartTime = "---",
                Status = delivery.DeliveryStatus ?? string.Empty,
                MessageType = delivery.MessageType ?? string.Empty,
                ErrorDetails = FormatErrorDetails(delivery.ErrorMessage),
                PhoneNumber = string.Empty,
                EmailAddress = delivery.EmailAddress ?? string.Empty,
                Type = "Delivery",
                SortTimestamp = ParseTimestamp(sent)
            });
        }

        var sortedMessages = combinedMessages
            .OrderByDescending(m => m.SortTimestamp)
            .ToList();

        dgMessages.ItemsSource = sortedMessages;
        UpdateSummary(sortedMessages);
    }

    private void UpdateSummary(List<MessageHistoryRow> messages)
    {
        // Update summary textblock: "# Total messages / # Successful / # Failures"
        try
        {
            var total = messages.Count;
            var failures = messages.Count(m => m.IsFailure);
            var successes = total - failures;

            // Build colored runs: total (black), successes (green), failures (red)
            txbkMessageSummary.Inlines.Clear();
            txbkMessageSummary.Inlines.Add(new Run(total.ToString()) { Foreground = Brushes.Black, FontWeight = FontWeights.SemiBold });
            txbkMessageSummary.Inlines.Add(new Run(" Total messages / ") { Foreground = Brushes.Black });
            txbkMessageSummary.Inlines.Add(new Run(successes.ToString()) { Foreground = Brushes.Green, FontWeight = FontWeights.SemiBold });
            txbkMessageSummary.Inlines.Add(new Run(" Successful / ") { Foreground = Brushes.Green });
            txbkMessageSummary.Inlines.Add(new Run(failures.ToString()) { Foreground = Brushes.Red, FontWeight = FontWeights.SemiBold });
            txbkMessageSummary.Inlines.Add(new Run(" Failures") { Foreground = Brushes.Red });
        }
        catch
        {
            // ignore if UI element not available or count fails
        }
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

    private void btnCopyAllMessageHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Preferred: ItemsSource is the List<MessageHistoryRow> we set in SetMessageData
            if (dgMessages.ItemsSource is IEnumerable<MessageHistoryRow> rows)
            {
                var contactHeader = _messageType == MessageHistoryType.SMS ? "Phone Number" : "Email Address";
                var headerNames = new[] { "Sent / Start Time", "Status", "Message Type", "Error/Details", contactHeader, "Type" };

                // Prepare rows
                var rowList = rows.Select(r => new[] {
                    r.SentOrStartTime ?? string.Empty,
                    r.Status ?? string.Empty,
                    r.MessageType ?? string.Empty,
                    r.ErrorDetails ?? string.Empty,
                    (_messageType == MessageHistoryType.SMS ? r.PhoneNumber : r.EmailAddress) ?? string.Empty,
                    r.Type ?? string.Empty
                }).ToList();

                // Column caps to avoid extremely wide output
                var colCaps = new[] { 30, 20, 30, 120, 20, 15 };

                // Compute widths based on header and content, respecting caps
                var widths = new int[headerNames.Length];
                for (int c = 0; c < headerNames.Length; c++)
                {
                    int max = headerNames[c].Length;
                    foreach (var r in rowList)
                        max = Math.Max(max, (r[c]?.Length) ?? 0);
                    widths[c] = Math.Min(max, colCaps[c]);
                }

                static string Truncate(string s, int w)
                {
                    if (s == null)
                        return string.Empty;
                    if (s.Length <= w)
                        return s;
                    if (w <= 3)
                        return s.Substring(0, w);
                    return s.Substring(0, w - 3) + "...";
                }

                var sb = new StringBuilder();

                // Header line
                for (int c = 0; c < headerNames.Length; c++)
                {
                    sb.Append(headerNames[c].PadRight(widths[c]));
                    if (c < headerNames.Length - 1)
                        sb.Append("  ");
                }
                sb.AppendLine();

                // Rows
                foreach (var r in rowList)
                {
                    for (int c = 0; c < r.Length; c++)
                    {
                        var cell = Escape(Truncate(r[c] ?? string.Empty, widths[c]));
                        sb.Append(cell.PadRight(widths[c]));
                        if (c < r.Length - 1)
                            sb.Append("  ");
                    }
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show(this, "Message history copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Fallback: iterate visible columns and items generically and produce padded text
            var headerCols = dgMessages.Columns.Select(c => c.Header?.ToString() ?? string.Empty).ToArray();
            var valuesMatrix = new List<string[]>();
            foreach (var item in dgMessages.Items)
            {
                if (item == null)
                    continue;

                var values = new List<string>();
                foreach (var col in dgMessages.Columns)
                {
                    if (col is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding && !string.IsNullOrEmpty(binding.Path?.Path))
                    {
                        var prop = item.GetType().GetProperty(binding.Path.Path);
                        var val = prop?.GetValue(item)?.ToString() ?? string.Empty;
                        values.Add(val);
                    }
                    else
                    {
                        values.Add(item.ToString() ?? string.Empty);
                    }
                }
                valuesMatrix.Add(values.ToArray());
            }

            if (headerCols.Length == 0)
            {
                Clipboard.SetText(string.Empty);
                return;
            }

            var widths2 = new int[headerCols.Length];
            for (int c = 0; c < headerCols.Length; c++)
            {
                int max = headerCols[c].Length;
                foreach (var r in valuesMatrix)
                    max = Math.Max(max, (r[c]?.Length) ?? 0);
                // apply some generic caps
                int cap = 120;
                if (c == 0)
                    cap = 30; // time
                if (c == headerCols.Length - 1)
                    cap = 15; // type
                if (c == headerCols.Length - 2)
                    cap = 20; // phone
                widths2[c] = Math.Min(max, cap);
            }

            var sbFallback = new StringBuilder();
            for (int c = 0; c < headerCols.Length; c++)
            {
                sbFallback.Append(headerCols[c].PadRight(widths2[c]));
                if (c < headerCols.Length - 1)
                    sbFallback.Append("  ");
            }
            sbFallback.AppendLine();

            foreach (var r in valuesMatrix)
            {
                for (int c = 0; c < r.Length; c++)
                {
                    var cell = Escape(r[c] ?? string.Empty);
                    var truncated = cell.Length > widths2[c] ? cell.Substring(0, widths2[c] - 3) + "..." : cell;
                    sbFallback.Append(truncated.PadRight(widths2[c]));
                    if (c < r.Length - 1)
                        sbFallback.Append("  ");
                }
                sbFallback.AppendLine();
            }

            Clipboard.SetText(sbFallback.ToString());
            MessageBox.Show(this, "Message history copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // In case clipboard fails or reflection fails, notify user without throwing.
            MessageBox.Show(this, $"Failed to copy message history: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string Escape(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
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
    public string EmailAddress { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime SortTimestamp { get; set; }

    // Combined property used by the DataGrid column: prefer Sent when present, otherwise show Start Time
    public string SentOrStartTime =>
        !string.IsNullOrWhiteSpace(Sent) && Sent != "---"
            ? Sent
            : (string.IsNullOrWhiteSpace(ScheduleStartTime) ? "---" : ScheduleStartTime);
}
