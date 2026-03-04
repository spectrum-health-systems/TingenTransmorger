[Tingen Transmorger manual](README.md)

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.29.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>TINGEN TRANSMORGER MANUAL</h1>

  THIS DOCUMENTATION IS A WORK-IN-PROGRESS

</div>

## Contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [How it works](#how-it-works)
- [Additional documentation](#additional-documentation)
- [Installing](#installing)
- [Using](#using)

# Introduction

Welcome to the [Tingen Transmorger](https://github.com/spectrum-health-systems/TingenTransmorger) manual!

Tingen Transmorger is a utility that transmorgifies data from [Netsmart's TeleHealth](https://www.ntst.com/carefabric/careguidance-solutions/telehealth) platform. and makes it easier to troubleshoot TeleHealth issues.

# Requirements

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- A 64-bit Operating System

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

# Installing

Please see the [Installing Tingen Transmorger](Transmorger-Installing.md) documentation.

# Using

Please see the [Using Tingen Transmorger](Transmorger-Using.md) documentation.

***

[Tingen Transmorger manual](README.md)

> <sub>Last updated: 260304</sub>

<!--


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
-->

