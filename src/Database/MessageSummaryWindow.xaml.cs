using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TingenTransmorger.Database;

/// <summary>
/// Interaction logic for MessageSummaryWindow.xaml
/// </summary>
public partial class MessageSummaryWindow : Window
{
    public MessageSummaryWindow()
    {
        InitializeComponent();
    }

    /// <summary>Sets the message data to display and populates the grids.</summary>
    /// <param name="smsFailures">List of SMS failure records.</param>
    /// <param name="messageDeliveries">List of message delivery records.</param>
    public void SetMessageData(
        List<(string ErrorMessage, string ScheduledStartTime)> smsFailures,
        List<(string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> messageDeliveries)
    {
        // Convert tuples to display objects for SMS failures
        var smsFailureItems = smsFailures
            .Select(f => new SmsFailureItem
            {
                ErrorMessage = f.ErrorMessage,
                ScheduledStartTime = f.ScheduledStartTime
            })
            .OrderByDescending(f => f.ScheduledStartTime)
            .ToList();

        dgSmsFailures.ItemsSource = smsFailureItems;

        // Convert tuples to display objects for message deliveries
        var messageDeliveryItems = messageDeliveries
            .Select(d => new MessageDeliveryItem
            {
                DeliveryStatus = d.DeliveryStatus,
                MessageType = d.MessageType,
                ErrorMessage = d.ErrorMessage,
                DateSent = d.DateSent,
                TimeSent = d.TimeSent
            })
            .OrderByDescending(d => d.DateSent)
            .ThenByDescending(d => d.TimeSent)
            .ToList();

        dgMessageDeliveries.ItemsSource = messageDeliveryItems;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    /// <summary>Display class for SMS failure records.</summary>
    private class SmsFailureItem
    {
        public string ScheduledStartTime { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>Display class for message delivery records.</summary>
    private class MessageDeliveryItem
    {
        public string DateSent { get; set; } = string.Empty;
        public string TimeSent { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

