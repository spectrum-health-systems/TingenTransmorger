// 260212_code
// 2260311_documentation

namespace TingenTransmorger.Core;

/// <summary>Provides static string array catalogs for application message box content.</summary>
/// <remarks>
/// <para>
/// Each method returns a two-element <see cref="string"/> array where index <c>0</c> contains the message
/// box title and index <c>1</c> contains the message body.
/// </para>
/// </remarks>
class Catalog
{
    /// <summary>Returns message box content for an invalid or undefined configuration setting.</summary>
    /// <param name="setting">The name of the configuration setting that is undefined.</param>
    /// <returns>
    /// A two-element array containing the message box title at index <c>0</c> and an error description with remediation
    /// instructions at index <c>1</c>.
    /// </returns>
    internal static string[] msgbox_InvalidConfigurationSetting(string setting) =>
    [
        $"Tingen Transmorger - File system error",
        $"The {setting} configuration setting is undefined.{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Please set a valid {setting} value in the configuration file."
    ];

    /// <summary>Returns message box content for a directory path that does not exist, prompting the user to create it.</summary>
    /// <param name="dirKey">The configuration key name identifying the missing directory.</param>
    /// <param name="dirValue">The full directory path that does not exist.</param>
    /// <returns>
    /// A two-element array containing the message box title at index <c>0</c> and an error description with a
    /// create-or-close prompt at index <c>1</c>.
    /// </returns>
    internal static string[] msgbox_PathDoesNotExistWithCreatePrompt(string dirKey, string dirValue) =>
    [
        $"Tingen Transmorger - File system error",
        $"The {dirKey} path does not exist:{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"{dirValue}{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Would you like to create it now?{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Please note: If you select 'No', the application will close."
    ];

    /// <summary>Returns message box content prompting the user to confirm a database rebuild.</summary>
    /// <returns>
    /// A two-element array containing the message box title at index <c>0</c> and a confirmation prompt at index <c>1</c>.
    /// </returns>
    internal static string[] msgbox_DatabaseRebuildCheck() =>
    [
        $"Tingen Transmorger - Database rebuild",
        $"Would you like to rebuild the database?"
    ];

    /// <summary>Returns message box content notifying the user that a newer database version is available.</summary>
    /// <returns>
    /// A two-element array containing the message box title at index <c>0</c> and an upgrade prompt at index <c>1</c>.
    /// </returns>
    internal static string[] msgbox_DatabaseUpdateAvailable() =>
    [
        $"Tingen Transmorger - Database update",
        $"A newer database version is available.{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Would you like to upgrade?"
    ];

    /// <summary>Returns message box content confirming that the database upgrade completed successfully.</summary>
    /// <returns>
    /// A two-element array containing the message box title at index <c>0</c> and a success message at index <c>1</c>.
    /// </returns>
    internal static string[] msgbox_DatabaseUpdateSuccess() =>
    [
        $"Tingen Transmorger - Database update",
        $"Database upgraded successfully."
    ];
}