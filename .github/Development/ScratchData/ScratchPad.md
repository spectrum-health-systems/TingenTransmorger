

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

    MEETING DETAILS
    ---------------
  Participant Names: JESSICA WOLFORD;KIM HEGARTY;JOHN F RUFO;KEVIN ROBERTS;CHARLES W HALL;ELIZABETH PHINNEY;ROY ADAMS;JAMES HASHEM;MARJORIE BYRNE
