[Tingen Transmorger manual](README.md) ❰ TeleHealth Report Overview

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.29.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>
    TINGEN TRANSMORGER MANUAL<br>
    TeleHealth Reports Overview
  </h1>

</div>

## CONTENTS

- [Required reports](#required-reports)
- [Report names](#report-names)
- [Report date range](#report-date-range)
- [Capturing all data](#capturing-all-data)
- [Report aggregation](#report-aggregation)
- [Missing dates](#missing-dates)

## Required reports

In order for Transmorger to do what it does, and do it accurately, it needs these reports:

1. Visit Details
2. Message Failure
3. Message Delivery
4. Visit Stats

## Report names

Downloaded report names look like this:

```text
STQma_%Report_Name%_%StartDate_EndDate%.xlsx
```
Where:

- `%Report_Name%` is the name of the report (e.g., `Message_Delivery`).

- `%StartDate_EndDate%` is the date-range (e.g., `YYYYMMDD_YYYYMMDD`).

So if you run the "Visit Details" report for 5/1/2026 - 5/15/20206, the name of the report would be:

```text
STQma_Visit_Details_20260501_20260515.xlsx
```

## Report date range

Each report requires a ***Start Date*** and an ***End Date***.

You can run a report for a single day by setting the *Start Date* and *End Date* to the same day.

In order to troubleshoot TeleHealth for the month of May 2026, you would need the following reports:

```text
STQma_Visit_Details_20260501_20260531.xlsx
STQma_Message_Failure_20260501_20260531.xlsx
STQma_Message_Delivery_20260501_20260531.xlsx
STQma_Visit_Stats_20260501_20260531.xlsx
```

## Capturing all data

In order to capture all data for a date/date-range. is recommended that you run reports once that date/range has passed.

For example, to get all data for 5/1/2026 - 5/15/20206, run the report on 5/16/26.

## Report aggregation

Since Transmorger aggregates *all* of the reports in the Import/ folder, you can run reports for shorter date-ranges that add up to larger date-ranges.

For example, the following reports would *also* build data for all of May 2026:

```text
STQma_Visit_Details_20260501_20260531.xlsx

STQma_Message_Failure_20260501_20260515.xlsx
STQma_Message_Failure_20260516_20260531.xlsx

STQma_Message_Delivery_20260501_20260510.xlsx
STQma_Message_Delivery_20260511_20260520.xlsx
STQma_Message_Delivery_20260521_20260531.xlsx

STQma_Visit_Stats_20260501_20260510.xlsx
STQma_Visit_Stats_20260511_20260520.xlsx
STQma_Visit_Stats_20260521_20260530.xlsx
STQma_Visit_Stats_20260531_20260531.xlsx
```

## Missing dates

If a report for a specific date does not exist, that data will not be included in the Transmorger database. All other dates will be included.

For example, if you ran reports for **5/1/26 - 1/15/26** and **5/17/26 - 5/31/26**, but *not* for **5/16/26**, data would exist for all of May 2026 *except* for 5/16/26.

This could be resolved by running reports with a start *and* end date of 5/16/25, and adding that report the the Import/ folder.

***

[Tingen Transmorger manual](README.md) ❰ TeleHealth Report Overview

> <sub>Last updated: 260304</sub>
