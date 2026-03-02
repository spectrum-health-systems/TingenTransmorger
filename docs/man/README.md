<div align="center">

  ![Logo](.github/Logo/TransmorgerLogo-256x256.png)

  ![Release](https://img.shields.io/badge/version-0.9.28.0-teal)&nbsp;&nbsp;
  ![License](https://img.shields.io/badge/license-apache-blue)

  <h1>MANUAL</h1>

</div>

### Contents

- Introduction
- Installation
- Initial launch
- Configuration
- Create the initial database

# Introduction

This is the Tingen Transmorger Manual!

# Installation

Tingen Transmorger is a stand-along, portable application, so it's not *installed* in the tradtional sense. All you need to do is:

1. Download the latest [release](https://github.com/spectrum-health-systems/TingenTransmorger/releases)
2. Extract the `TingenTransmorger.exe` file to a location of your choice

That's it!

# Initial launch

The first time you launch Tingen Transmorger, it does a few setup-type things.

When you double-click on the `TingenTransmorger.exe` file, you should get a popup that looks like this:

![](./Images/LocalDbPathDoesNotExistError.png)

The **LocalDb path** is the path that the local Transmorger database will be located. This can be anywhere you want, and can be different for each end-user.

By default, the LocalDb path is `AppData/Database`.

Click **Yes**, and Transmorger will exit.

Now if you take a look in the folder where `TingenTransmorger.exe` is, you'll notice there is a new folder named `AppData`. This is where Transmorger will store various data that it needs to function...including the configuration file.

# Configuring Tingen Transmorger

Not only did Tingen Transmorger create the `AppData` folder...it also created the `AppData/Config` folder *and* the `AppData/Config/transmorger.config` configuration file!

Let's take a look at that file, and make some changes.

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
This is the location for the **master database**. The master database - which is named `transmorger.db`, by the way - is the most up-to-date version of the Transmorger database, and is must be located in a location where all end-users can access it.

### Admin directories

Admin mode uses two **additional** directories:

- **Tmp**  
Any temporary data that Transmorger needs to function is stored here. When Transmorger is executed for the first time, this is set to `AppData/Tmp`, which is the recommended location.

- **Import**  
This is the location for the TeleHealth reports that will be ***transmorgified***. This can be anywhere, but for organizational purposes I recommend putting it in the parent folder of the `MasterDb`.

## Modifying the configuration file

Now that we've gone over the contents of the the transmorger.config file, let's make some necessary changes, but not to the existing `LocalDb` and `Tmp` entries - let's leave those at their defaults.

For **standard** users, we are only going to modify the `MasterDb` setting.

For **admin** users, we are going to modify both the `MasterDb` and `Import` settings.

### Modifying the `MasterDb` location

Modify this component of the configuration file to point to where your master database will reside.

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

This change needs to be made for both *standard* and *admin* users.

### Modifying the `Import` location

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

# Initializing the Tingen Transmorger database

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




## Launch TingenTransmorger (again)

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