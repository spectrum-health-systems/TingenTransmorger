<div align="center">

  ![Logo](.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.28.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>Tingen Transmorger</h1>

</div>

Troubleshooting [Netsmart's TeleHealth](https://www.ntst.com/carefabric/careguidance-solutions/telehealth) platform can be frustrating; data is spread across multiple reports which use inconsistent syntax, and is not end-user friendly.

Tingen Transmorger is a utility that *aggregates* those reports, **transmorgifies** the data, and makes it easy to find specific patient, provider, or meeting information.

## Features

- The Transmorger database can contain multiple months of data
- Data can be added to the database on-the-fly, and available to end-users immediately
- The end-user database is updated automatically, ensuring users have the latest available data
- Specific data can be copied out of Transmorger, and pasted into tickets, emails, etc.

# How it works

There is detailed information in the [Transmorger Manual](), but essentially:

1. TeleHealth reports are (manually) run from the TeleHealth portal
2. The completed reports are saved to a location
3. Tingen Transmorger takes all of the reports in that location and *transmorgifies* it into a custom database containing all of the data from across all reports
4. That custom database is saved in a location that end-users have access to
5. End-users can run Tingen Transmorger

# Getting Started

Read the [Transmorger Manual]()!

# Development

Tingen Transmorger is being actively developed. The current development branch is [here]().

You can also take a look at the [roadmap](ROADMAP.md), [known issues](KNOWN-ISSUES.md), and [changelog](CHANGELOG.md)
