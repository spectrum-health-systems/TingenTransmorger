

private void btnSearchToggle_Click(object? sender, RoutedEventArgs e) => SearchToggleClicked();
private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => SearchTextChanged();
private void rbtnSearch_Checked(object sender, RoutedEventArgs e) => SearchTextChanged();
private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => SearchResultSelected();
private void dgPatientMeetings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
private void btnPhoneDetails_Click(object sender, RoutedEventArgs e) => PhoneDetailsClicked();
private void btnEmailDetails_Click(object sender, RoutedEventArgs e) => EmailDetailsClicked();
private void btnCopyMeetingDetailsGeneral_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsGeneralClicked();
private void btnCopyMeetingDetailsPatient_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsPatientClicked();
private void btnCopyMeetingDetailsProvider_Click(object sender, RoutedEventArgs e) => CopyMeetingDetailsProviderClicked();
   
   
    /// <summary>Displays the patient's phone numbers in the UI.</summary>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers = GetPatientPhoneNumbers(patientDetails);
        ShowPatientPhoneNumber(phoneNumbers);
        GetSmsStats(phoneNumbers);
        UpdateDetailsButtonColor(_smsFailures.Count > 0, _smsDeliveries.Count > 0, btnPhoneDetails);
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
        lblPatientPhoneValue.Content = phoneNumbers.Count > 0
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
                var failures = TmDb.GetSmsFailureStats(normalizedPhoneNumber);
                _smsFailures.AddRange(failures);

                var deliveries = TmDb.GetMessageDeliveryStats(normalizedPhoneNumber);
                _smsDeliveries.AddRange(deliveries);
            }
        }
    }