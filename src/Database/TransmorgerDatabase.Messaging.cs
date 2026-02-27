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
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

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

                // Get DeliveryFailure array
                if (!phoneEntry.TryGetProperty("DeliveryFailure", out var deliveryFailures))
                    continue;

                if (deliveryFailures.ValueKind != JsonValueKind.Array)
                    continue;

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

                    results.Add((formattedPhone, errorMessage, scheduledStart));
                }
            }
        }

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
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {

            return results;
        }

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

                // Get DeliverySuccess array
                if (!phoneEntry.TryGetProperty("DeliverySuccess", out var deliverySuccesses))
                    continue;

                if (deliverySuccesses.ValueKind != JsonValueKind.Array)
                    continue;

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


                    results.Add((formattedPhone, deliveryStatus, messageType, errorMessage, dateSent, timeSent));
                }
            }
        }

        return results;
    }

    /// <summary>Gets email failure records for a specific email address by searching all patients.</summary>
    /// <param name="emailAddress">The email address to search for.</param>
    /// <returns>A list of email failure records with Email Address, Error Message and Scheduled Start Time.</returns>
    public List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> GetEmailFailureStats(string emailAddress)
    {
        var results = new List<(string, string, string)>();

        if (!_hasData || string.IsNullOrWhiteSpace(emailAddress))
            return results;

        // Get the Patients array from the root
        if (!_jsonRoot.TryGetProperty("Patients", out var patients))
        {
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        // Iterate through all patients
        foreach (var patient in patients.EnumerateArray())
        {
            // Check if patient has EmailAddresses array
            if (!patient.TryGetProperty("EmailAddresses", out var emailAddresses))
                continue;

            if (emailAddresses.ValueKind != JsonValueKind.Array)
                continue;

            // Check each email address
            foreach (var emailEntry in emailAddresses.EnumerateArray())
            {
                // Get the email address
                if (!emailEntry.TryGetProperty("Address", out var addressElement))
                    continue;

                var emailAddressFromJson = addressElement.GetString();
                if (string.IsNullOrWhiteSpace(emailAddressFromJson))
                    continue;

                // Check if this email address matches (case insensitive)
                if (!emailAddressFromJson.Equals(emailAddress, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Get DeliveryFailure array
                if (!emailEntry.TryGetProperty("DeliveryFailure", out var deliveryFailures))
                    continue;

                if (deliveryFailures.ValueKind != JsonValueKind.Array)
                    continue;

                // Extract each failure record
                foreach (var failure in deliveryFailures.EnumerateArray())
                {
                    var errorMessage = failure.TryGetProperty("ErrorMessage", out var errElem)
                        ? errElem.GetString() ?? string.Empty
                        : string.Empty;

                    var scheduledStart = failure.TryGetProperty("ScheduledStart", out var startElem)
                        ? startElem.GetString() ?? string.Empty
                        : string.Empty;

                    results.Add((emailAddressFromJson, errorMessage, scheduledStart));
                }
            }
        }

        return results;
    }

    /// <summary>Gets email delivery records for a specific email address by searching all patients.</summary>
    /// <param name="emailAddress">The email address to search for.</param>
    /// <returns>A list of email delivery records with Email Address, Delivery Status, Message Type, Error Message, Date Sent, and Time Sent.</returns>
    public List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> GetEmailDeliveryStats(string emailAddress)
    {
        var results = new List<(string, string, string, string, string, string)>();

        if (!_hasData || string.IsNullOrWhiteSpace(emailAddress))
            return results;

        // Get the Patients array from the root
        if (!_jsonRoot.TryGetProperty("Patients", out var patients))
        {
            return results;
        }

        if (patients.ValueKind != JsonValueKind.Array)
        {
            return results;
        }


        // Iterate through all patients
        foreach (var patient in patients.EnumerateArray())
        {
            // Check if patient has EmailAddresses array
            if (!patient.TryGetProperty("EmailAddresses", out var emailAddresses))
                continue;

            if (emailAddresses.ValueKind != JsonValueKind.Array)
                continue;

            // Check each email address
            foreach (var emailEntry in emailAddresses.EnumerateArray())
            {
                // Get the email address
                if (!emailEntry.TryGetProperty("Address", out var addressElement))
                    continue;

                var emailAddressFromJson = addressElement.GetString();
                if (string.IsNullOrWhiteSpace(emailAddressFromJson))
                    continue;

                // Check if this email address matches (case insensitive)
                if (!emailAddressFromJson.Equals(emailAddress, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Get DeliverySuccess array
                if (!emailEntry.TryGetProperty("DeliverySuccess", out var deliverySuccesses))
                    continue;

                if (deliverySuccesses.ValueKind != JsonValueKind.Array)
                    continue;

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

                    results.Add((emailAddressFromJson, deliveryStatus, messageType, errorMessage, dateSent, timeSent));
                }
            }
        }

        return results;
    }
}
