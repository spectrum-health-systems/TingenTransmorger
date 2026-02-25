

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
   



    A few things of note:
    
    

    * The btnSearchToggle control toggles between patient and provider search modes.
    
    * When in patient search mode:
    - The brdrMeetingDetailsPatient control is visible
    - The spnlPatientPhoneAndEmail control is visible
    - The brdrMeetingDetailsProvider control is hidden
    
    * When in provider search mode:
    - The spnlPatientPhoneAndEmail control is hidden
    - The brdrMeetingDetailsProvider control is visible
    - The brdrMeetingDetailsPatient control is hidden
    
    * If there are any errors with a meeting, the Meeting ID cell for that meeting will be highlighted in LightSalmon.
    
    * If a patient has a phone number and/or email address, the user can click the btnPhoneDetails or btnPhoneDetails
    buttons the to view more details about those pieces of information. These buttons will be different colors,
    depending on the following:
    - If the details are all success messages, the buttons will have a green background
    - If the details are all failure messages, the buttons will have a red background
    - If the details are a mix of success and failure messages, the buttons will have an orange background
    - If the patient has a phone number/email address, but there are no details to show, the buttons will have a gray background
    - If the patient does not have a phone number/email address, the buttons will have a black background
    
    - There are various "copy" buttons that copy different pieces of information to the clipboard.