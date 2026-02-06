# Tingen Muno: Development Notes

- [X] Building the database when it exists in the master directory = error.
- [ ] Getting latest master DB
- [ ] Break up TransmorgerDatabase.cs
- [ ] Clean up Database classes
- [X] Change version to vx.x.x.x
- [ ] Details for phone messages
- [ ] Details for email messages
- [ ] Details for all messages
- [ ] Make sure controls clear when they are supposed to
- [ ] Collapse window components correctly
- [ ] Smaller final datbase
- [ ] Show building process
- [ ] SHFB


## Settings
- [ ] Check for database updates at startup
- [ ] Show data base statistics/summaries
- [ ] Ignore null ids
- [ ] Limit date range (dropdown: 1/3/6/12 months)


Method signatures
Why multiple provider ids?
Rename _Visit_Details-Patient_Meeting_Specifics.json
Rename _Visit_Details-Provider_Meeting_Specifics.json
Compress database for transfering
Make sure all paths use Path.Combine
logic exists for missing local database, but not for missing master database?
Reg users alway download the latest databases
Trim().ToLower() everything
What other stuff can be combined
ConvertSmsStatsWorksheet() and ConvertEmailStatsWorksheet()
The ConvertMessageDeliveryStatsWorksheet() method is different than the others
move translation stuff (e.g., maps_, first/last reverses) to excel conversion
Do something to shrink database size
MunoDatabase ok global?
Remove leading "1" on phone numbers
Database contains a list of files used to build that version
Public/internal/private, static
Admin loads database
Verify files are ONE_TWO-THREE_FOUR
Open excel files for detailed research
If there are excel files missing, things crash