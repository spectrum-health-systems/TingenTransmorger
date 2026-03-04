[Tingen Transmorger manual](README.md) ❰ Transmorger Setup

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.29.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>
    TINGEN TRANSMORGER MANUAL<br>
    Installing Transmorger
  </h1>

</div>

### CONTENTS

- [Introduction](#introduction)
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

# Introduction

This document will walk you through installing Tingen Transmorger.

> [!IMPORTANT]
> Before continuing, please make sure you have reviewed the [requirements](README.md#requirements), and have read the [additional documentation](README.md#additional-documentation).

# Installation

Tingen Transmorger is a stand-alone, portable, and (in theory) cross-platform application.

To install Transmorger, just:

1. Download the latest [release](https://github.com/spectrum-health-systems/TingenTransmorger/releases)
2. Extract the `TingenTransmorger.exe` file to a location of your choice

> [!WARNING]
> Verify the SHA256 hash (v0.9.29.0)  
> `---`



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

> [!TIP]
> You may want to refresh your knowledge of the following before continuing:  
> - The Transmorger [Configuration](TransmorgerConfigurationOverview.md) file  
> - The [MasterDb](TransmorgerDatabaseOverview.md#the-master-database)  
> - The [LocalDb](TransmorgerDatabaseOverview.md#the-local-database)  

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

This change only needs to be made for both *admin* users.

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





***

[Tingen Transmorger manual](README.md) ❰ Transmorger Setup

> <sub>Last updated: 260304</sub>
