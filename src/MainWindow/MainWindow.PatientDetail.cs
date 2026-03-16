// 260227_code
// 260311_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/* The MainWindow.PatientDetails partial class contains logic related to displaying patient details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary>The name of the currently selected patient.</summary>
    /// <value>Set when a patient is selected; defaults to <see cref="string.Empty"/>.</value>
    private string _currentPatientName = string.Empty;

    /// <summary>The ID of the currently selected patient.</summary>
    /// <value>Set when a patient is selected; defaults to <see cref="string.Empty"/>.</value>
    private string _currentPatientId = string.Empty;

    /// <summary>SMS delivery records for the current patient's phone numbers.</summary>
    /// <value>Populated by <see cref="GetSmsStats"/>; cleared before each reload.</value>
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _smsDeliveries = [];

    /// <summary>Email delivery records for the current patient's email addresses.</summary>
    /// <value>Populated by <see cref="GetEmailStats"/>; cleared before each reload.</value>
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries = [];

    /// <summary>SMS failure records for the current patient's phone numbers.</summary>
    /// <value>Populated by <see cref="GetSmsStats"/>; cleared before each reload.</value>
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures =[];

    /// <summary>Email failure records for the current patient's email addresses.</summary>
    /// <value>Populated by <see cref="GetEmailStats"/>; cleared before each reload.</value>
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures = [];

    /// <summary>Loads and displays all patient detail sections in the UI for the specified patient.</summary>
    /// <remarks>Caches the patient name and ID in instance fields before delegating to the display methods.</remarks>
    /// <param name="patientName">The full name of the patient to display.</param>
    /// <param name="patientId">The unique identifier of the patient to display.</param>
    private void DisplayPatientDetails(string patientName, string patientId)
    {
        // TODO: Depreciate these and just pass the actual values.
        _currentPatientName = patientName;
        _currentPatientId   = patientId;

        JsonElement? patientDetails = _tmDb.GetPatientDetails(patientName, patientId);

        if (patientDetails == null)
        {
            StopApp($"Critical error! [ERR-MW8000]");
        }

        SetPatientDetailUi(patientName, patientId);
        DisplayPatientPhoneNumber(patientDetails);
        DisplayPatientEmailAddress(patientDetails);
        DisplayPatientMeetingResults(patientDetails);
    }

    /// <summary>Retrieves phone numbers from patient data, displays them, and updates the phone detail button color.</summary>
    /// <remarks>Updates SMS delivery and failure statistics as a side effect via <see cref="GetSmsStats"/>.</remarks>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers = GetPatientPhoneNumbers(patientDetails);

        ShowPatientPhoneNumber(phoneNumbers);
        GetSmsStats(phoneNumbers);
        UpdateDetailsButtonColor(_smsFailures.Count > 0, _smsDeliveries.Count > 0, btnUserPhoneDetail);
    }

    /// <summary>Extracts and formats phone numbers from the patient's JSON data.</summary>
    /// <remarks>Formats ten-digit numbers as ###-###-####; other lengths are included without formatting.</remarks>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    /// <returns>A list of phone number strings extracted and formatted from the patient's JSON data.</returns>
    private static List<string> GetPatientPhoneNumbers(JsonElement? patientDetails)
    {
        var phoneNumbers = new List<string>();

        if (patientDetails?.TryGetProperty("PhoneNumbers", out var phoneNumbersArray) == true && phoneNumbersArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var phoneNumberEntry in phoneNumbersArray.EnumerateArray())
            {
                if (phoneNumberEntry.TryGetProperty("Number", out var number))
                {
                    var phoneNumber = number.GetString();

                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        var phoneNumberDigits = new string(phoneNumber.Where(char.IsDigit).ToArray());

                        if (phoneNumberDigits.Length == 10)
                        {
                            phoneNumber = $"{phoneNumberDigits.Substring(0, 3)}-{phoneNumberDigits.Substring(3, 3)}-{phoneNumberDigits.Substring(6, 4)}"; // Format as ###-###-#### if 10 digits
                        }

                        phoneNumbers.Add(phoneNumber);
                    }
                }
            }
        }

        return phoneNumbers;
    }

    /// <summary>Displays the patient's phone numbers in the UI label, or a placeholder if none exist.</summary>
    /// <remarks>Displays 'No phone numbers on file' if <paramref name="phoneNumbers"/> is empty.</remarks>
    /// <param name="phoneNumbers">A list of formatted phone numbers for the patient.</param>
    private void ShowPatientPhoneNumber(List<string> phoneNumbers)
    {
        lblUserPhoneValue.Content = phoneNumbers.Count > 0
            ? string.Join(", ", phoneNumbers)
            : "No phone numbers on file";
    }

    /// <summary>Clears and repopulates the SMS failure and delivery record lists for the given phone numbers.</summary>
    /// <remarks>Only processes numbers with exactly ten digits after normalizing to digits only.</remarks>
    /// <param name="phoneNumbers">A list of formatted phone numbers for the patient.</param>
    private void GetSmsStats(List<string> phoneNumbers)
    {
        _smsFailures.Clear();
        _smsDeliveries.Clear();

        for (int i = 0; i < phoneNumbers.Count; i++)
        {
            var normalizedPhoneNumber = new string(phoneNumbers[i].Where(char.IsDigit).ToArray()).Trim();

            if (normalizedPhoneNumber.Length == 10)
            {
                List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> failures = _tmDb.GetSmsFailureStats(normalizedPhoneNumber);
                _smsFailures.AddRange(failures);

                List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> deliveries = _tmDb.GetMessageDeliveryStats(normalizedPhoneNumber);
                _smsDeliveries.AddRange(deliveries);
            }
        }
    }

    /// <summary>Loads email addresses for the current patient and updates the email UI and detail button.</summary>
    /// <remarks>Populates email statistics as a side effect via <see cref="GetEmailStats"/> and updates the button color.</remarks>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientEmailAddress(JsonElement? patientDetails)
    {
        var emailAddresses = GetPatientEmailAddresses(patientDetails);

        ShowPatientEmailAddress(emailAddresses);
        GetEmailStats(emailAddresses);
        UpdateDetailsButtonColor(_emailFailures.Count > 0, _emailDeliveries.Count > 0, btnUserEmailDetail);
    }

    /// <summary>Extracts email addresses from the patient's JSON data.</summary>
    /// <remarks>Only non-empty, non-whitespace addresses are included in the returned list.</remarks>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    /// <returns>A list of email address strings extracted from the patient's JSON data.</returns>
    private static List<string> GetPatientEmailAddresses(JsonElement? patientDetails)
    {
        var emailAddresses = new List<string>();

        if (patientDetails.Value.TryGetProperty("EmailAddresses", out var emailAddressesArray))
        {
            if (emailAddressesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var emailEntry in emailAddressesArray.EnumerateArray())
                {
                    if (emailEntry.TryGetProperty("Address", out var addressElem))
                    {
                        var address = addressElem.GetString();

                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            emailAddresses.Add(address);
                        }
                    }
                }
            }
        }

        return emailAddresses;
    }

    /// <summary>Clears and repopulates the email failure and delivery record lists for the given addresses.</summary>
    /// <remarks>Skips the placeholder value 'No email addresses on file' when processing.</remarks>
    /// <param name="emailAddresses">A list of email addresses for the patient.</param>
    private void GetEmailStats(List<string> emailAddresses)
    {
        _emailFailures.Clear();
        _emailDeliveries.Clear();

        foreach (var emailAddress in emailAddresses)
        {
            if (emailAddress != "No email addresses on file")
            {
                List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> failures = _tmDb.GetEmailFailureStats(emailAddress);

                _emailFailures.AddRange(failures);

                List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> deliveries = _tmDb.GetEmailDeliveryStats(emailAddress);

                _emailDeliveries.AddRange(deliveries);
            }
        }
    }

    /// <summary>Displays the patient's email addresses in the UI label, or a placeholder if none exist.</summary>
    /// <remarks>Mutates <paramref name="emailAddresses"/> by appending a placeholder when the list is empty.</remarks>
    /// <param name="emailAddresses">A list of email addresses for the patient.</param>
    private void ShowPatientEmailAddress(List<string> emailAddresses)
    {
        if (emailAddresses.Count == 0)
        {
            emailAddresses.Add("No email addresses on file");
        }

        lblUserEmailValue.Content = string.Join(", ", emailAddresses);
    }

    /// <summary>Opens the message history window for either phone or email message records.</summary>
    /// <remarks>No dialog is shown if <paramref name="messageType"/> does not match 'phone' or 'email'.</remarks>
    /// <param name="messageType">The message channel to display; either 'phone' or 'email'.</param>
    private void ShowMessageDetails(string messageType)
    {
        MessageHistoryWindow messageHistoryWindow;

        if (messageType == "phone")
        {
            messageHistoryWindow = new MessageHistoryWindow(_smsFailures, _smsDeliveries) { Owner = this };
            messageHistoryWindow.ShowDialog();
        }
        else if (messageType == "email")
        {
            messageHistoryWindow = new MessageHistoryWindow(_emailFailures, _emailDeliveries) { Owner = this };
            messageHistoryWindow.ShowDialog();
        }
    }
}