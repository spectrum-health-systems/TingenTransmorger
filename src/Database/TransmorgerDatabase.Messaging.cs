// 260206_code
// 260206_documentation

using System.Text.Json;

namespace TingenTransmorger.Database;

/// <summary>Partial class for TransmorgerDatabase to add SMS tracking methods.</summary>
public partial class TransmorgerDatabase
{
    /// <summary>Gets SMS failure records for a specific phone number by searching all patients.</summary>
    /// <param name="phoneNumber">The normalized 10-digit phone number to search for.</param>
    /// <returns>A list of SMS failure records with Phone Number, Error Message and Scheduled Start Time.</returns>
    public List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> GetSmsFailureStats(string phoneNumber)
    {
        var results = new List<(string, string, string)>();
        
        if (!_hasData || string.IsNullOrWhiteSpace(phoneNumber))
            return results;

        // Get the Patients array from the root
        if (!_jsonRoot.TryGetProperty("Patients", out var patients))
        {
            System.Diagnostics.Debug.WriteLine("Patients property not found in database root");
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {
            System.Diagnostics.Debug.WriteLine($"Patients is not an array, it's a {patients.ValueKind}");
            return results;
        }

        System.Diagnostics.Debug.WriteLine($"Searching {patients.GetArrayLength()} patients for phone number {phoneNumber}");

        // Iterate through all patients
        foreach (var patient in patients.EnumerateArray())
        {
            // Check if patient has PhoneNumbers array
            if (!patient.TryGetProperty("PhoneNumbers", out var phoneNumbers))
                continue;

            if (phoneNumbers.ValueKind != JsonValueKind.Array)
                continue;

            // Check each phone number
            foreach (var phoneEntry in phoneNumbers.EnumerateArray())
            {
                // Get the phone number
                if (!phoneEntry.TryGetProperty("Number", out var numberElement))
                    continue;

                var phoneNumberFromJson = numberElement.GetString();
                if (string.IsNullOrWhiteSpace(phoneNumberFromJson))
                    continue;

                // Normalize the phone number from JSON (remove non-digits and leading 1)
                var normalizedPhoneFromJson = new string(phoneNumberFromJson.Where(char.IsDigit).ToArray());
                if (normalizedPhoneFromJson.Length == 11 && normalizedPhoneFromJson[0] == '1')
                {
                    normalizedPhoneFromJson = normalizedPhoneFromJson.Substring(1);
                }

                // Check if this phone number matches
                if (normalizedPhoneFromJson != phoneNumber)
                    continue;

                System.Diagnostics.Debug.WriteLine($"Found matching phone number: {phoneNumberFromJson}");

                // Get DeliveryFailure array
                if (!phoneEntry.TryGetProperty("DeliveryFailure", out var deliveryFailures))
                    continue;

                if (deliveryFailures.ValueKind != JsonValueKind.Array)
                    continue;

                System.Diagnostics.Debug.WriteLine($"  Found {deliveryFailures.GetArrayLength()} delivery failures");

                // Extract each failure record
                foreach (var failure in deliveryFailures.EnumerateArray())
                {
                    var errorMessage = failure.TryGetProperty("ErrorMessage", out var errElem)
                        ? errElem.GetString() ?? string.Empty
                        : string.Empty;
                    
                    var scheduledStart = failure.TryGetProperty("ScheduledStart", out var startElem)
                        ? startElem.GetString() ?? string.Empty
                        : string.Empty;

                    // Format the phone number for display
                    var formattedPhone = normalizedPhoneFromJson.Length == 10
                        ? $"{normalizedPhoneFromJson.Substring(0, 3)}-{normalizedPhoneFromJson.Substring(3, 3)}-{normalizedPhoneFromJson.Substring(6, 4)}"
                        : phoneNumberFromJson;

                    System.Diagnostics.Debug.WriteLine($"    Adding failure: {errorMessage} at {scheduledStart}");
                    results.Add((formattedPhone, errorMessage, scheduledStart));
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total SMS failures found: {results.Count}");
        return results;
    }

    /// <summary>Gets message delivery records for a specific phone number by searching all patients.</summary>
    /// <param name="phoneNumber">The normalized 10-digit phone number to search for.</param>
    /// <returns>A list of message delivery records with Phone Number, Delivery Status, Message Type, Error Message, Date Sent, and Time Sent.</returns>
    public List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> GetMessageDeliveryStats(string phoneNumber)
    {
        var results = new List<(string, string, string, string, string, string)>();
        
        if (!_hasData || string.IsNullOrWhiteSpace(phoneNumber))
            return results;

        // Get the Patients array from the root
        if (!_jsonRoot.TryGetProperty("Patients", out var patients))
        {
            System.Diagnostics.Debug.WriteLine("Patients property not found in database root");
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {
            System.Diagnostics.Debug.WriteLine($"Patients is not an array, it's a {patients.ValueKind}");
            return results;
        }

        System.Diagnostics.Debug.WriteLine($"Searching {patients.GetArrayLength()} patients for phone number {phoneNumber}");

        // Iterate through all patients
        foreach (var patient in patients.EnumerateArray())
        {
            // Check if patient has PhoneNumbers array
            if (!patient.TryGetProperty("PhoneNumbers", out var phoneNumbers))
                continue;

            if (phoneNumbers.ValueKind != JsonValueKind.Array)
                continue;

            // Check each phone number
            foreach (var phoneEntry in phoneNumbers.EnumerateArray())
            {
                // Get the phone number
                if (!phoneEntry.TryGetProperty("Number", out var numberElement))
                    continue;

                var phoneNumberFromJson = numberElement.GetString();
                if (string.IsNullOrWhiteSpace(phoneNumberFromJson))
                    continue;

                // Normalize the phone number from JSON (remove non-digits and leading 1)
                var normalizedPhoneFromJson = new string(phoneNumberFromJson.Where(char.IsDigit).ToArray());
                if (normalizedPhoneFromJson.Length == 11 && normalizedPhoneFromJson[0] == '1')
                {
                    normalizedPhoneFromJson = normalizedPhoneFromJson.Substring(1);
                }

                // Check if this phone number matches
                if (normalizedPhoneFromJson != phoneNumber)
                    continue;

                System.Diagnostics.Debug.WriteLine($"Found matching phone number: {phoneNumberFromJson}");

                // Get DeliverySuccess array
                if (!phoneEntry.TryGetProperty("DeliverySuccess", out var deliverySuccesses))
                    continue;

                if (deliverySuccesses.ValueKind != JsonValueKind.Array)
                    continue;

                System.Diagnostics.Debug.WriteLine($"  Found {deliverySuccesses.GetArrayLength()} successful deliveries");

                // Extract each delivery record
                foreach (var delivery in deliverySuccesses.EnumerateArray())
                {
                    var deliveryStatus = delivery.TryGetProperty("DeliveryStatus", out var statusElem)
                        ? statusElem.GetString() ?? string.Empty
                        : string.Empty;
                    
                    var messageType = delivery.TryGetProperty("MessageType", out var typeElem)
                        ? typeElem.GetString() ?? string.Empty
                        : string.Empty;
                    
                    var errorMessage = delivery.TryGetProperty("ErrorMessage", out var errElem)
                        ? errElem.GetString() ?? string.Empty
                        : string.Empty;
                    
                    var dateSent = delivery.TryGetProperty("DateSent", out var dateElem)
                        ? dateElem.GetString() ?? string.Empty
                        : string.Empty;
                    
                    var timeSent = delivery.TryGetProperty("TimeSent", out var timeElem)
                        ? timeElem.GetString() ?? string.Empty
                        : string.Empty;

                    // Format the phone number for display
                    var formattedPhone = normalizedPhoneFromJson.Length == 10
                        ? $"{normalizedPhoneFromJson.Substring(0, 3)}-{normalizedPhoneFromJson.Substring(3, 3)}-{normalizedPhoneFromJson.Substring(6, 4)}"
                        : phoneNumberFromJson;

                    System.Diagnostics.Debug.WriteLine($"    Adding delivery: {messageType} - {deliveryStatus} on {dateSent} at {timeSent}");
                    results.Add((formattedPhone, deliveryStatus, messageType, errorMessage, dateSent, timeSent));
                }
            }
        }


        System.Diagnostics.Debug.WriteLine($"Total message deliveries found: {results.Count}");
        return results;
    }
}
