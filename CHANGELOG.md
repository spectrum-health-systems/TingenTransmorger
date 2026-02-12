# Tingen Transmorger: Changelog

### 0.9.25.4

- `ADDED` MainWindow/
- `ADDED` MainWindow.UserInterface.cs
- 'REFACTOR` Made significant changes to the following classes:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - TransmorgerDatabase.cs

### 0.9.25.3

- 'REFACTOR` Made significant changes to the following classes:
  - MainWindow.asmx.cs
  - MainWindow.AdminMode.cs
  - TransmorgerDatabase.cs
- `ADDED` Additional MsgBox components to Core.Catalog.cs

### 0.9.25.2

- `MODIFIED` The default configuration file now defines both the LocalDb and Tmp directories under /AppData
- `MODIFIED` MainWindow
  - Commented out some code that may not be needed
  - Created MainWindow.AdminMode.cs partial class for admin mode stuff
  - Minor refactoring/code/comment cleanup
- `MODIFIED` DatabaseRebuildWindow
  - Minor UI tweaks

### 0.9.25.1

- `REMOVED` Unused/abandoned code

## 0.9.25.0 - 2/11/2026

- `ADDED` Meeting Details (Provider) component
- `MODIFIED` Disabled the meeting search functionality for now

### 0.9.24.0

- `ADDED` Functionality to copy Meeting Details (General)
- `ADDED` Functionality to copy Meeting Details (Patient)
- `REMOVED` DiagnosticWindow
- `REMOVED` EmailSummaryWindow

### 0.9.23.0

- `MODIFIED` Migrated the EmailSummaryWindow functionality into MessageHistoryWindow

### 0.9.22.0

- `ADDED` Functionality to copy the following message histories to the clipboard:
  - All message history
  - The top 10 rows of message history
  - All successes
  - All errors
- `ADDED` TingenTransmorger.Help.HelpWindow
- `MODIFIED` TingenTransmorger.Database.MessageHistoryWindow
  - Renamed TingenTransmorger.Database.MessageSummaryWindow -> TingenTransmorger.Database.MessageHistoryWindow
  - Minor changes to user interface

### 0.9.21.0

- `ADDED` ProjectInfo.cs
- `FIXED` The "opted-out" message properly displays

## 0.9.20.0 - 2/10/2026

- `FIXED` Fixed an issue where a clean install without a local database would not download the master database.
