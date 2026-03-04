s[Tingen Transmorger manual](README.md)

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.29.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>TINGEN TRANSMORGER MANUAL</h1>

</div>

## Contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [How it works](#how-it-works)
- [Additional documentation](#additional-documentation)

- [Installation](#installation)
- [Initial setup](#initial-launch)
  - [Setup-type thing #1: Creating the LocalDb path](#setup-type-thing-1-creating-the-localdb-path)
  - [Setup-type thing #2: The MasterDb path](#setup-type-thing-2-the-masterdb-path)
- [Configuration](#configuration)
- [Initializing the Master Transmorger database](#initializing-the-master-transmorger-database)
- [Using Transmorger]()

# Introduction

Welcome to the [Tingen Transmorger](https://github.com/spectrum-health-systems/TingenTransmorger) manual!

Tingen Transmorger is a utility that aggregates data from [Netsmart's TeleHealth](https://www.ntst.com/carefabric/careguidance-solutions/telehealth) platform. and makes it easier to troubleshoot TeleHealth issues.

# Requirements

Tingen Transmorger requires [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), so make sure that the .NET Desktop Runtime (or the SDK, if you aren't into the whole brevity thing) is installed.

In addition, Transmorger is a 64-bit application, and will not run on 32-bit machines.

# How it works

Here's the 50,000-foot view of how Tingen Transmorger works:

- TeleHealth reports are (manually) run from the TeleHealth portal
- The completed reports are downloaded
- Transmorger takes all of the downloaded reports and ***transmorgifies*** them into a single, custom database
- That custom database is saved in a location that end-users have access to
- Transmorger automatically downloads/updates the database for end-users
- End-users can use Transmorger to troubleshoot TeleHealth issues

# Additional documentation

Before you continue, I would recommend taking a quick look at the following documentation, so you are familiar with various terminology:

- [Transmorger Database Overview](TransmorgerDatabaseOverview.md)
- [Transmorger Configuration Overview](TransmorgerConfigurationOverview.md)
- [TeleHealth Reports Overview](TeleHealthReportsOverview.md)









If you take a look in the folder where `TingenTransmorger.exe` is, you'll notice there is a folder named `AppData`, which is where Transmorger will store various data that it needs to function.

You'll also see the `AppData/Database` folder that was created for the [LocalDb](#setup-type-thing-1-creating-the-localdb-path).

We're interested in other folder here: `AppData/Config`, which contains the transmorger.config` configuration file.

Let's take a look at that file, and make some changes.

# Initializing the Master Transmorger database

That last thing was only *mostly* true: Tingen Transmorger needs one more configuration change, but it's a temporary one.

We need to change the **Mode** to "Admin", so we can build the initial Transmorger database.

So open the `transmorger.config` file, and change this line:

```json
    "Mode": "Standard",
```

...to this:

```json
    "Mode": "Admin",
```

...then save the configuration file.

But don't launch Transmorger yet! To build the Transmorger database, we need TeleHealth reports.

## Downloading the TeleHealth reports

So...the TeleHealth reports that Transmorger needs to build its database need to be downloaded manually.

Yeah, not great. But it is what it is.

#### The reports

--> link to thing

Each report needs a date range, and the date range should be the same for all reports.

#### Running a report

To run a TeleHealth reports:

1. Login to your TeleHealth portal
2. Click the "Reports" tab
3. Choose a report to run
4. Choose the start and end date of the report
5. Click "Run Report"

Using the "Message Delivery Report" as an example:

![](./Images/TransmorgerManual-TeleHealthReportDownload-01.jpg)

Some reports take longer than others, and some reports take pretty long.

While the report is being run, you'll see a "Processing" button (that's disabled).

![](./Images/TransmorgerManual-TeleHealthReportDownload-02.jpg)

> [!NOTE]
> All TeleHealth reports in the Import folder are used to create the Transmorger database, so you can download incremental date ranges.
>
> For example, running two reports from 1/1/2026-1/15/2015 and 1/16/20206-1/31/2026 will give you the same result as running a single report from 1/1/2026-1/31/2-26.

#### Downloading a report

Once the "Processing" button becomes the Download" button (which is enabled), download the report to your "Import" directory.

![](./Images/TransmorgerManual-TeleHealthReportDownload-03.jpg)

Once all reports have been downloaded, we can launch Tingen Transmorger and initialize the database.

### Creating the Master Transmorger database

First, confirm that the `transmorger.config` file has the following line:

```json
    "Mode": "Admin",
```

Then, launch Transmorger.

You'll get the following popup:

![](./Images/TransmorgerManual-MasterDbRebuildPrompt.png)

Click **Yes** to initialize the Transmorger database (which, technically, is just "rebuilding" it for the first time).

While the database is being built, you'll see a progress indicator:

![](./Images/TransmorgerManual-RebuildingDbProgress.png)

When the build process is complete, you'll see a popup letting you know there is a database update available.

![](./Images/TransmorgerManual-NewerDbAvailablePrompt.png)

> [!NOTE]
> When you rebuild the Transmorger database, you are rebuilding the **master** database.
>
> Once that is complete, Transmorger checks the local version of the database to see if it's older than the master (which, in this case, it is), and prompts you to update.

Since we want that update, click **Yes**

You will then (hopefully) get a popup letting you know the database has been updated.

![](./Images/TransmorgerManual-UpdateDbSuccess.png)

Click "OK", then click the "Close" button on the "Rebuilding Transmorger Database" window.

![](./Images/TransmorgerManual-RebuildDbCompleteClose.png)

Tingen Transmorger will then launch.





# Using Transmorger
























Run `TingenTransmorger.exe` again.

If you didn't manually create `AppData/Database`, Transmorger will prompt you to create it now:

![](.github/Readme/LocalDbDoesNotExistError.png)

Either way, you'll get this popup letting you know that there is a newer version of the database (since the local version doesn't actually exist yet):

![](.github/Readme/NewerDatabaseAvailable.png)

Click "Yes", wait a few seconds (hopefully), and then you should get this message:

![](.github/Readme/DatabaseUpgradeSuccess.png)

Click "Ok", and you'll see the Transmorger Main Window:

![](.github/Readme/TransmorgerMainWindow.png)








And here's a secret: *it doesn't have to be local*. That's right, you 

Tmp/ cleaning


***

[Tingen Transmorger manual](README.md)

> <sub>Last updated: 260304</sub>
