# Tingen Muno: Development Notes

- [ ] Break up TransmorgerDatabase.cs
- [ ] Clean up Database classes
- [ ] Details for email messages
- [ ] Details for all messages
- [ ] Make sure controls clear when they are supposed to
- [ ] Smaller final database
- [ ] SHFB
- [ ] Ignore null ids (provider "no id", e.g., Lori D.)
- [ ] Show data base statistics/summaries
- [ ] After rebuild, non-existant database causes error
- [ ] Version on title bar
- [ ] Do the MeetingBreakdownComponents need to have text when launching?
- [ ] What controls have text when launching that don't need text?
- [ ] Tighter search (e.g., "scott" finds "Scott" or "Scotty", not "Ascott")

- [ ] Review dgPatientProviderMeetings to make sure the comments work for both patients and providers

- [ ] Make sure all of this is accurate:
    * If a patient has a phone number and/or email address, the user can click the btnPhoneDetails or btnPhoneDetails
    buttons the to view more details about those pieces of information. These buttons will be different colors, 
    depending on the following:
    - If the details are all success messages, the buttons will have a green background
    - If the details are all failure messages, the buttons will have a red background
    - If the details are a mix of success and failure messages, the buttons will have an orange background
    - If the patient has a phone number/email address, but there are no details to show, the buttons will have a gray background
    - If the patient does not have a phone number/email address, the buttons will have a black background


- [X] Details for phone messages
- [X] Add "-" to phone numbers
- [X] Remove leading "+1" on phone numbers
- [X] Change version to vx.x.x.x
- [X] Building the database when it exists in the master directory = error.
- [X] Getting latest master DB
- [X] Collapse window components correctly
- [X] Show building process
- [X] Check for database updates at startup




- Is verification working? if no import in config, crash

Method signatures
Make sure all paths use Path.Combine
Trim().ToLower() everything
Do something to shrink database size
Database contains a list of files used to build that version
Public/internal/private, static
Verify files are ONE_TWO-THREE_FOUR
Open excel files for detailed research



<a target="_blank" href="https://icons8.com/icon/43011/copy">Copy</a> icon by <a target="_blank" href="https://icons8.com">Icons8</a>


