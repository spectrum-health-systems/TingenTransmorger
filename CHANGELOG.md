# Tingen Transmorger: Changelog

## 0.9.28.0 - 2/27/25

> There were a lot of under-the-hood changes/updates for this release, focusing on cleaning up the code and making things clearer.

`ADDED` Catalog information for copies to the clipboard  
`MODIFIED` Moved '''ReplaceNulls''' helper function to ```ReplaceNullValues()```  
`MODIFIED` Background border (but needs more work)  
`UPDATED` Fixed alignments  
`UPDATED` Verified all controls are named correctly  
`UPDATED` Commented all controls  
`FIXED` Admin mode background border color  
`FIXED` Meeting participant names display  
`FIXED` UI reset when changing search modes  
`REFACTORED` Moved most stuff out of MainWindow.xaml.cs to other partial classes  
`REFACTORED` MainWindow.AdminMode.cs  
`REFACTORED` MainWindow.DataCopy.cs  
`REFACTORED` MainWindow.MeetingDetails.cs  
`REFACTORED` MainWindow.PatientDetail.cs  
`REFACTORED` MainWindow.ProviderDetail.cs  
`REFACTORED` MainWindow.Search.cs  
`REFACTORED` MainWindow.UserDetail.cs  
`REFACTORED` MainWindow.UserInterface.cs  
`REFACTORED` MainWindow.xaml  
`REMOVED` Provider email logic  
`REMOVED` System.Diagnostics.Debug statements  
`REMOVED` MainWindow.DisplayDetails.cs  
`REMOVED` MainWindow.Events.cs  

## 0.9.27.0 - 2/18/25

- `MODIFIED` Cleaned up MainWindow.xaml

## 0.9.26.0 - 2/17/25

- `ADDED` MainWindow/
  - Moved MainWindow classes to MainWindow/
- `ADDED` MainWindow.UserInterface.cs
- `ADDED` Additional MsgBox components to Core.Catalog.cs
- `MODIFIED` The default configuration file now defines both the LocalDb and Tmp directories under /AppData
- `MODIFIED` MainWindow
  - Commented out some code that may not be needed
  - Created MainWindow.AdminMode.cs partial class for admin mode stuff
  - Minor refactoring/code/comment cleanup
- `MODIFIED` DatabaseRebuildWindow
  - Minor UI tweaks
- `REFACTORED` Combined the patient and provider search methods into a single method
- `REFACTORED` Made significant changes to the following classes:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - TransmorgerDatabase.cs
- `REMOVED` Settings button
- `REMOVED` Unused/abandoned code

## 0.9.25.0 - 2/11/2026

- `ADDED` Meeting Details (Provider) component
- `MODIFIED` Disabled the meeting search functionality for now

## 0.9.24.0 - 2/11/2026

- `ADDED` Functionality to copy Meeting Details (General)
- `ADDED` Functionality to copy Meeting Details (Patient)
- `REMOVED` DiagnosticWindow
- `REMOVED` EmailSummaryWindow

## 0.9.23.0 - 2/11/2026

- `MODIFIED` Migrated the EmailSummaryWindow functionality into MessageHistoryWindow

## 0.9.22.0 - 2/11/2026

- `ADDED` Functionality to copy the following message histories to the clipboard:
  - All message history
  - The top 10 rows of message history
  - All successes
  - All errors
- `ADDED` TingenTransmorger.Help.HelpWindow
- `MODIFIED` TingenTransmorger.Database.MessageHistoryWindow
  - Renamed TingenTransmorger.Database.MessageSummaryWindow -> TingenTransmorger.Database.MessageHistoryWindow
  - Minor changes to user interface

## 0.9.21.0 - 2/11/2026

- `ADDED` ProjectInfo.cs
- `FIXED` The "opted-out" message properly displays

## 0.9.20.0 - 2/10/2026

- `FIXED` Fixed an issue where a clean install without a local database would not download the master database.
