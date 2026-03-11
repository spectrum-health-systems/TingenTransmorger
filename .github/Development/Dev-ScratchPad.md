// 260227_code
// 260227_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/* The MainWindow.PatientDetails partial class contains logic related to displaying patient details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary> Currently selected patient name.</summary>
    private string _currentPatientName = string.Empty;

    /// <summary>Currently selected patient ID.</summary>
    private string _currentPatientId = string.Empty;

    /// <summary>Message delivery records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _smsDeliveries = [];

    /// <summary>Email delivery records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries = [];

    /// <summary>SMS failure records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures =[];

    /// <summary>Email failure records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures = [];

    /// <summary>Displays patient details in the UI.</summary>
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
        DisplayPatientMeetingResults(patientDetails); // The meeting results box
    }

    /// <summary>Displays the patient's phone numbers in the UI.</summary>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers = GetPatientPhoneNumbers(patientDetails);

        ShowPatientPhoneNumber(phoneNumbers);
        GetSmsStats(phoneNumbers);
        UpdateDetailsButtonColor(_smsFailures.Count > 0, _smsDeliveries.Count > 0, btnUserPhoneDetail);
    }

    /// <summary>Get a list of formatted phone numbers for a patient.</summary>
    /// <param name="patientDetails">The JSON representation of the patient's details.</param>
    /// <returns>A list of strings representing the formatted phone numbers of the patient.</returns>
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

    /// <summary>Show the patient phone number.</summary>
    /// <param name="phoneNumbers">A list of formatted phone numbers for the patient.</param>
    private void ShowPatientPhoneNumber(List<string> phoneNumbers)
    {
        lblUserPhoneValue.Content = phoneNumbers.Count > 0
            ? string.Join(", ", phoneNumbers)
            : "No phone numbers on file";
    }

    /// <summary>Gets the SMS statistics for the provided phone numbers.</summary>
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

    /// <summary>Gets the patient's email addresses from the patient details and displays them in the UI.</summary>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientEmailAddress(JsonElement? patientDetails)
    {
        var emailAddresses = GetPatientEmailAddresses(patientDetails);

        ShowPatientEmailAddress(emailAddresses);
        GetEmailStats(emailAddresses);
        UpdateDetailsButtonColor(_emailFailures.Count > 0, _emailDeliveries.Count > 0, btnUserEmailDetail);
    }

    /// <summary>
    /// Retrieves a list of email addresses associated with the specified patient details.
    /// </summary>
    /// <remarks>This method extracts email addresses from the provided JSON object. It only includes
    /// non-empty addresses in the returned list.</remarks>
    /// <param name="patientDetails">A JSON element representing the patient's details. This element is expected to contain an 'EmailAddresses'
    /// property with an array of email address objects. This parameter cannot be null.</param>
    /// <returns>A list of strings containing the email addresses of the patient. The list will be empty if no email addresses
    /// are found.</returns>
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

    /// <summary>Gets the email statistics for the provided email addresses.</summary>
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

    /// <summary>Shows the patient's email addresses in the UI.</summary>
    /// <param name="emailAddresses">A list of email addresses for the patient.</param>
    private void ShowPatientEmailAddress(List<string> emailAddresses)
    {
        if (emailAddresses.Count == 0)
        {
            emailAddresses.Add("No email addresses on file");
        }

        lblUserEmailValue.Content = string.Join(", ", emailAddresses);
    }

    /// <summary>Displays the message history for the specified message type, either phone or email.</summary>
    /// <param name="messageType">The type of message.</param>
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