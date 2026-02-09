// 260206_code
// 260206_documentation

using System.Text.Json;

namespace TingenTransmorger.Database;

/// <summary>Partial class for TransmorgerDatabase to add SMS tracking methods.</summary>
public partial class TransmorgerDatabase
{
    /// <summary>Gets SMS failure records for a specific phone number.</summary>
    /// <param name="phoneNumber">The normalized 10-digit phone number to search for.</param>
    /// <returns>A list of SMS failure records with Error Message and Scheduled Start Time.</returns>
    public List<(string ErrorMessage, string ScheduledStartTime)> GetSmsFailureStats(string phoneNumber)
    {
        var results = new List<(string, string)>();
        
        if (!_hasData || string.IsNullOrWhiteSpace(phoneNumber))
            return results;

        // Navigate to Summary first
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
        {
            System.Diagnostics.Debug.WriteLine("Summary property not found in database root");
            return results;
        }

        if (!summary.TryGetProperty("MessageFailure", out var messageFailure))
        {
            System.Diagnostics.Debug.WriteLine("MessageFailure property not found in Summary");
            return results;
        }
        
       // MessageFailure is directly the SmsStats array
       if (messageFailure.ValueKind != JsonValueKind.Array)
        {
           System.Diagnostics.Debug.WriteLine($"MessageFailure is not an array, it's a {messageFailure.ValueKind}");
            return results;
        }

       System.Diagnostics.Debug.WriteLine($"MessageFailure (SmsStats) array has {messageFailure.GetArrayLength()} clients");

       foreach (var client in messageFailure.EnumerateArray())
        {
            if (!client.TryGetProperty("Records", out var records))
            {
                System.Diagnostics.Debug.WriteLine("Client has no Records property");
                continue;
            }

            if (records.ValueKind != JsonValueKind.Array)
                continue;

            System.Diagnostics.Debug.WriteLine($"  Client has {records.GetArrayLength()} records");

            foreach (var record in records.EnumerateArray())
            {
                // Debug: Show all property names in this record
                var propertyNames = new List<string>();
                foreach (var prop in record.EnumerateObject())
                {
                    propertyNames.Add(prop.Name);
                }
                System.Diagnostics.Debug.WriteLine($"    Record properties: {string.Join(", ", propertyNames)}");

                if (record.TryGetProperty("To ", out var toElement))
                {
                    var toNumber = toElement.GetString();
                    System.Diagnostics.Debug.WriteLine($"    To field value: '{toNumber}'");
                    
                    if (!string.IsNullOrWhiteSpace(toNumber))
                    {
                        // Normalize the phone number from JSON (remove non-digits and leading 1)
                        var normalizedToNumber = new string(toNumber.Where(char.IsDigit).ToArray());
                        if (normalizedToNumber.Length == 11 && normalizedToNumber[0] == '1')
                        {
                            normalizedToNumber = normalizedToNumber.Substring(1);
                        }

                        System.Diagnostics.Debug.WriteLine($"    Comparing normalized '{normalizedToNumber}' with '{phoneNumber}'");

                        if (normalizedToNumber == phoneNumber)
                        {
                            var errorMessage = record.TryGetProperty("Error Message", out var errElem)
                                ? errElem.GetString() ?? string.Empty
                                : string.Empty;
                            
                            var scheduledStart = record.TryGetProperty("Scheduled Start Time", out var startElem)
                                ? startElem.GetString() ?? string.Empty
                                : string.Empty;

                            System.Diagnostics.Debug.WriteLine($"    MATCH FOUND! Error: {errorMessage}, Start: {scheduledStart}");
                            results.Add((errorMessage, scheduledStart));
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("    No 'To ' field found in record");
                }
            }
        }

        return results;
    }

    /// <summary>Gets message delivery records for a specific phone number.</summary>
    /// <param name="phoneNumber">The normalized 10-digit phone number to search for.</param>
    /// <returns>A list of message delivery records with Delivery Status, Message Type, Error Message, Date Sent, and Time Sent.</returns>
    public List<(string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> GetMessageDeliveryStats(string phoneNumber)
    {
        var results = new List<(string, string, string, string, string)>();
        
        if (!_hasData || string.IsNullOrWhiteSpace(phoneNumber))
            return results;

        // Navigate to Summary first
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
        {
            System.Diagnostics.Debug.WriteLine("Summary property not found in database root");
            return results;
        }

        if (!summary.TryGetProperty("MessageDelivery", out var messageDelivery))
        {
            System.Diagnostics.Debug.WriteLine("MessageDelivery property not found in Summary");
            return results;
        }
        
        // MessageDelivery is directly the MessageDeliveryStats array
        if (messageDelivery.ValueKind != JsonValueKind.Array)
        {
            System.Diagnostics.Debug.WriteLine($"MessageDelivery is not an array, it's a {messageDelivery.ValueKind}");
            return results;
        }

        System.Diagnostics.Debug.WriteLine($"MessageDelivery array has {messageDelivery.GetArrayLength()} records");

        int recordIndex = 0;
        foreach (var record in messageDelivery.EnumerateArray())
        {
            recordIndex++;
            
            // Debug: Show all property names in this record (only for first few records)
            if (recordIndex <= 3)
            {
                var propertyNames = new List<string>();
                foreach (var prop in record.EnumerateObject())
                {
                    propertyNames.Add(prop.Name);
                }
                System.Diagnostics.Debug.WriteLine($"  Record {recordIndex} properties: {string.Join(", ", propertyNames)}");
            }

            // Filter for SMS messages only
            if (record.TryGetProperty("Delivery Type", out var deliveryTypeElement))
            {
                var deliveryType = deliveryTypeElement.ValueKind == JsonValueKind.String 
                    ? deliveryTypeElement.GetString() 
                    : null;
                
                if (recordIndex <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  Record {recordIndex} Delivery Type: '{deliveryType}'");
                }

                // Only process SMSMessage records
                if (deliveryType != "SMSMessage")
                {
                    if (recordIndex <= 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Skipping non-SMS record");
                    }
                    continue;
                }
            }
            else
            {
                if (recordIndex <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  Record {recordIndex} has no 'Delivery Type' field, skipping");
                }
                continue;
            }

            if (record.TryGetProperty("Phone Number", out var phoneNumberElement))
            {
                var phoneNumberValue = phoneNumberElement.ValueKind == JsonValueKind.String 
                    ? phoneNumberElement.GetString() 
                    : null;
                
                if (recordIndex <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  Record {recordIndex} Phone Number: '{phoneNumberValue}'");
                }
                
                if (!string.IsNullOrWhiteSpace(phoneNumberValue))
                {
                    // Normalize the phone number from JSON (remove non-digits and leading 1)
                    var normalizedPhoneNumber = new string(phoneNumberValue.Where(char.IsDigit).ToArray());
                    if (normalizedPhoneNumber.Length == 11 && normalizedPhoneNumber[0] == '1')
                    {
                        normalizedPhoneNumber = normalizedPhoneNumber.Substring(1);
                    }

                    if (recordIndex <= 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Normalized: '{normalizedPhoneNumber}' vs '{phoneNumber}'");
                    }

                    if (normalizedPhoneNumber == phoneNumber)
                    {
                        var deliveryStatus = record.TryGetProperty("Delivery Status", out var statusElem) && statusElem.ValueKind == JsonValueKind.String
                            ? statusElem.GetString() ?? string.Empty
                            : string.Empty;
                        
                        var messageType = record.TryGetProperty("Message Type", out var typeElem) && typeElem.ValueKind == JsonValueKind.String
                            ? typeElem.GetString() ?? string.Empty
                            : string.Empty;
                        
                        var errorMessage = record.TryGetProperty("Error Message", out var errElem) && errElem.ValueKind == JsonValueKind.String
                            ? errElem.GetString() ?? string.Empty
                            : string.Empty;
                        
                        var dateSent = record.TryGetProperty("Date Sent", out var dateElem) && dateElem.ValueKind == JsonValueKind.String
                            ? dateElem.GetString() ?? string.Empty
                            : string.Empty;
                        
                        var timeSent = record.TryGetProperty("Time Sent", out var timeElem) && timeElem.ValueKind == JsonValueKind.String
                            ? timeElem.GetString() ?? string.Empty
                            : string.Empty;

                        System.Diagnostics.Debug.WriteLine($"  MATCH FOUND! Status: {deliveryStatus}, Type: {messageType}");
                        results.Add((deliveryStatus, messageType, errorMessage, dateSent, timeSent));
                    }
                }
            }
            else
            {
                if (recordIndex <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  Record {recordIndex} has no 'Phone Number' field");
                }
            }
        }

        return results;
    }
}
