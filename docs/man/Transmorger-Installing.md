[Tingen Transmorger manual](README.md) ❰ Installing Transmorger

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.30.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>
    TINGEN TRANSMORGER MANUAL<br>
    Installing Transmorger
  </h1>

</div>

### CONTENTS

- [Installation](#installation)
- [Initial setup](#initial-launch)
  - [Setup-type thing #1: Creating the LocalDb path](#setup-type-thing-1-creating-the-localdb-path)
  - [Setup-type thing #2: The MasterDb path](#setup-type-thing-2-the-masterdb-path)
- [Configuration](#configuration)
  - [Modifying the configuration file](#modifying-the-configuration-file)
  - [Modifying the MasterDb location](#modifying-the-masterdb-location)
  - [Modifying the Import location](#modifying-the-import-location)
- [Initializing the Master Transmorger database](#initializing-the-master-transmorger-database)
- [Using Transmorger]()

***

> [!IMPORTANT]
> Before continuing, please make sure you have reviewed the [requirements](README.md#requirements), and have read the [additional documentation](README.md#additional-documentation).

***

# Installation

Tingen Transmorger is a stand-alone, portable, and (in theory) cross-platform application.

To install Transmorger, just:

1. Download the latest [release](https://github.com/spectrum-health-systems/TingenTransmorger/releases)
2. Extract the `TingenTransmorger.exe` file to a location of your choice

> [!WARNING]
> Verify the SHA256 hash!  
> ```text
> Name: TingenTransmorger-0.9.29.0.7z
> Size: 41526296 bytes : 39 MiB
> SHA256: bb397045b775c87c432de98f3dd928da7c8424dcd15da62abb31f543c7dadfed
> ```

# Initial setup

When you double-click on the `TingenTransmorger.exe` file, and launch it for the first time, it does a few setup-type things.

## Setup-type thing #1: Creating the LocalDb path

The first thing you should see when you first launch Transmorger is this popup:

<div align="center">

![](./Images/TransmorgerManual-LocalDbPathDoesNotExistCreatePrompt.png)

</div>

The ***LocalDb path*** is where the *local copy* of the Transmorger database will stored.

When you click **Yes**, Transmorger will create an empty folder named `./AppData/Database`. This is the default (and recommended) location for the LocalDb, but you can change the path to any location via the configuration file.

Click **Yes**.

> [!WARNING]
> Clicking **No** will exit Transmorger.  
> Subsequent launches will ask the same question, until you click **Yes**, so this step is required.

## Setup-type thing #2: The MasterDb path

Next, another message should popup:

<div align="center">

![](./Images/TransmorgerManual-MasterDbPathIsUndefined.png)

</div>

The **MasterDb** is the most up-to-date version of the Transmorger database...but it doesn't actually exist yet. In fact, it doesn't even have a *location* to exist in!

We'll fix that next, so for now just click **OK**, and Transmorger will exit.

# Configuration

If you take a look in the folder where `TingenTransmorger.exe` is, you'll notice there is a folder named `AppData`, which is where Transmorger will store various data that it needs to function.

You'll also see the `AppData/Database` folder that was *just created* for the [LocalDb](#setup-type-thing-1-creating-the-localdb-path).

We're interested in other folder here: `AppData/Config`, which contains the `transmorger.config` configuration file.

Let's open that file, and make some modifications.

## Modifying the configuration file

We are going to make the following changes to the `transmorger.config`:

- For **standard** users, we are only going to modify the ***MasterDb*** setting.
- For **admin** users, we are going to modify both the ***MasterDb*** and ***Import*** settings.

Notice that we're leaving the existing ***LocalDb*** and ***Tmp*** defaults.

## Modifying the MasterDb location

The ***MasterDb*** component of the configuration file needs to point to where your master database will reside.

So this:

```json
    "MasterDb": ""
```

...becomes this:

```json
    "MasterDb": "path/to/database"
```

...or a more real-world example:

```json
    "MasterDb": "Z:/Transmorger/Database"
```

## Modifying the `Import` location

Modify this component of the configuration file to point to where all TeleHealth reports will downloaded.

So this:

```json
    "Import": ""
```

...becomes this:

```json
    "MasterDb": "path/to/imports"
```

...or a more real-world example:

```json
    "MasterDb": "Z:/Transmorger/Import"
```

## Saving the configuration file

Your modified `transmorger.config` file should look something like this:

```json
{
  "Mode": "Standard",
  "StandardDirectories": {
    "LocalDb": "AppData/Database",
    "MasterDb": "Z:/Transmorger/Database"
  },
  "AdminDirectories": {
    "Tmp": "AppData/Tmp",
    "Import": "Z:/Transmorger/Database"
  }
}
```

Save the changes.

Tingen Transmorger is now configured!

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

# Running the TeleHealth reports

For detailed instruction on how to run and download TeleHealth reports, please see the [TeleHealth reports](TeleHealth-Reports.md#running-reports) documentation.

# Creating the Master Transmorger database

Now that we have the necessary TeleHealth reports, launch Transmorger.

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

Click *OK*, then click the "Close" button on the "Rebuilding Transmorger Database" window.

![](./Images/TransmorgerManual-RebuildDbCompleteClose.png)

Tingen Transmorger will then launch in Admin mode.

Exit Transmorger, and put it back into "Standard" by modifying the configuration file from this:

```json
    "Mode": "Admin",
```

...to this:

```json
    "Mode": "Standard",
```

That's it! Transmorger is now ready to use!

***

[Tingen Transmorger manual](README.md) ❰ Installing Transmorger

> <sub>Last updated: 260305</sub>
