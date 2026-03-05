# Tingen Transmorger: Roadmap

## Misc

- Create an Tingen Transmorger icon (currently it's the default, boring application icon)

## Updated/modified functionality

- The phone and email buttons should be more descriptive than just changing colors (maybe Success/Failure text, e.g., "2/9")
- Determine what to do with users with null IDs, since *technically* these include valid users
- Determine what do do with users that have invalid names, since *technically* these include valid users
- If a meeting error is over a certain number of characters, potentially display it separately (either a MsgBox or window)
- If participant data is over a certain number of characters, potentially display it separately (either a MsgBox or window)
- Tighter searching (e.g., "scott" finds "Scott" or "Scotty", not "Ascott")
- Auto close the database update window (low priority, since this is only in admin mode)
- Standardize Lastname, Firstname
- Fix {} provider IDs

## New functionality

- Show Visit-Stats and Message-Delivery statistics in a separate window
- Show total users found at bottom of user list
- Ability to open the original Excel files for further research
- Integrate SHFB

## Refactor

- Providers are listed as PatientName in the database (I think)
- Everything in ns:Database needs to be refactored.
- MainWindow.MeetingDetails.cs needs to be refactored.
- MainWindow.PatientDetails.cs needs to be refactored.
- MainWindow.ProviderDetails.cs needs to be refactored.
- Look into making the database smaller
- Version on title bar
- Icon
- Remove all text on controls in xaml
- Use explicit types when possible
- Make sure error messages are where they should be
- brdrMainWindow is wonky
- Verify parameters use the same wording
- Verify the same wording is used across methods/comments (e.g., "Clear" instead of "Clears")
- Move the catalog stuff out of MainWindow.DataCopy.cs and into Core.Catalog.cs
- BuildMeetingList() should return a list, not reference it.
- Verify all paths use Path.Combine
- Verify method signatures are the same
- Trim().ToLower() everything, or use the comparison stuff
- Public/internal/private, static
- The try...catch/if block in private async Task StartApp() should probably be moved to Database.TransmorgerDatabase.cs