[Tingen Transmorger manual](README.md) ❰ Transmorger Configuration Overview

***

<div align="center">

  ![Logo](../../.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.29.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>
    TINGEN TRANSMORGER MANUAL<br>
    Transmorger Configuration Overview
  </h1>

</div>

## CONTENTS

- [The Transmorger configuration file]()

## The Tranmorger Configuration file

The Transmorger **configuration file**:

- is named `transmorger.config`
- is located in `AppData/Configuration` (by default)
- is stored as a standard, prettified JSON file
- contains the settings that Transmorger needs to run

## The default configuration file

The default `transmorger.config` file looks like this:

```json
{
  "Mode": "Standard",
  "StandardDirectories": {
    "LocalDb": "AppData/Database",
    "MasterDb": ""
  },
  "AdminDirectories": {
    "Tmp": "AppData/Tmp",
    "Import": ""
  }
}
```

There are three components to the configuration file:

- Mode
- StandardDirectories
- AdminDirectories

### Mode

There are two modes that Transmorger can run in:

- **Standard**  
This is the mode that end-users should always use.

- **Admin**  
This mode is used for rebuilding the Transmorger database, and is *not* intended for end-users. You can find more information about this mode [here]().

### Standard directories

Standard mode uses two directories:

- **LocalDb**  
This is the location for the end-users local Transmorger database. As you can see, when Transmorger is executed for the first time, and the configuration file is created, this is set to the default (and recommended) `AppData/Database`.

- **MasterDb**  
This is the location for the **master database**. The master database is the most up-to-date version of the Transmorger database, and is must be located in a location where all end-users can access it.

### Admin directories

Admin mode uses two **additional** directories:

- **Tmp**  
Any temporary data that Transmorger needs to function is stored here. When Transmorger is executed for the first time, this is set to `AppData/Tmp`, which is the recommended location.

- **Import**  
This is the location for the TeleHealth reports that will be ***transmorgified***. This can be anywhere, but for organizational purposes I recommend putting it in the parent folder of the `MasterDb`.

[Tingen Transmorger manual](README.md) ❰ Transmorger Configuration Overview

> <sub>Last updated: 260304</sub>
