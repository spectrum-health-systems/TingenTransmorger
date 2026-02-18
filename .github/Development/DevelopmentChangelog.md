# Tingen Transmorger: Changelog

## 0.9.27.2 - 0.9.27.11

- Cleanup of:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - MainWindow.DetailDisplay.cs
  - MainWindow.Events.cs
  - MainWindow.UserInterface.cs
- Fixed the UI reset when changing search modes
- Cleaned up the copied details

## 0.9.27.1

- Code/comment cleanup

## 0.9.27.0

- Finalized MainWindow.axml cleanup

## 0.9.26.1 - 0.9.26.7

- Continued to clean up MainWindow.axml

## 0.9.26.0

- Removed Settings button
- Started to cleanup MainWindow.axml

## 0.9.25.8

- Working on breaking the patient display logic up into more manageable pieces.

## 0.9.25.7

- Combined the patient and provider search methods into a single method

## 0.9.25.5 - 0.9.25.6

- Made significant changes to the following classes:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - TransmorgerDatabase.cs

## 0.9.25.3 - 0.9.25.4

- MainWindow/
  - Moved MainWindow classes to MainWindow/
- MainWindow.UserInterface.cs
- Made significant changes to the following classes:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - TransmorgerDatabase.cs

## 0.9.25.2

- The default configuration file now defines both the LocalDb and Tmp directories under /AppData
- MainWindow
  - Commented out some code that may not be needed
  - Created MainWindow.AdminMode.cs partial class for admin mode stuff
  - Minor refactoring/code/comment cleanup
- DatabaseRebuildWindow
  - Minor UI tweaks

## 0.9.25.1

- Unused/abandoned code

## 0.9.25.0 - 2/11/2026

- Meeting Details (Provider) component
- Disabled the meeting search functionality for now

## 0.9.24.0

- Functionality to copy Meeting Details (General)
- Functionality to copy Meeting Details (Patient)
- DiagnosticWindow
- EmailSummaryWindow

## 0.9.23.0

- Migrated the EmailSummaryWindow functionality into MessageHistoryWindow

## 0.9.22.0

- Functionality to copy the following message histories to the clipboard:
  - All message history
  - The top 10 rows of message history
  - All successes
  - All errors
- TingenTransmorger.Help.HelpWindow
- TingenTransmorger.Database.MessageHistoryWindow
  - Renamed TingenTransmorger.Database.MessageSummaryWindow -> TingenTransmorger.Database.MessageHistoryWindow
  - Minor changes to user interface

## 0.9.21.0

- ProjectInfo.cs
- The "opted-out" message properly displays

## 0.9.20.0 - 2/10/2026

- Fixed an issue where a clean install without a local database would not download the master database.
