// 260212_code
// 260212_documentation

namespace TingenTransmorger.Core;

/// <summary>Pre-defined text blocks and message strings.</summary>
/// <remarks>
/// <para>
/// Centralized to keep the code clean, maintainable, and to facilitate modification of messages.
/// </para>
/// <para>
/// The following types of blueprints are defined here:
/// <list type="bullet">
///     <item><b><c>   lst_</c></b> - A list of something</item>
///     <item><b><c>msgbox_</c></b> - Message box string[] definitions</item>
///     <item><b><c>msgtxt_</c></b> - Text string definitions</item>
/// </list>
/// </para>
/// </remarks>
class Catalog
{
    /* =========================================================================
     * msgbox_
     *
     * There are two types of text blocks defined here:
     *
     * 1. string
     *
     * 2. string[]
     *    These strings are returned as string arrays, where:
     *      [0] = Message box title
     *      [1] = Message box content
     * =========================================================================
     */

    internal static string[] msgbox_InvalidConfigurationSetting(string setting) =>
    [
        $"Tingen Transmorger - File system error",
        $"The {setting} configuration setting is undefined.{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Please set a valid {setting} value in the configuration file."
    ];

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

    internal static string[] msgbox_DatabaseRebuildCheck() =>
    [
        $"Tingen Transmorger - Database rebuild",
        $"Would you like to rebuild the database?"
    ];

    internal static string[] msgbox_DatabaseUpdateAvailable() =>
    [
        $"Tingen Transmorger - Database update",
        $"A newer database version is available.{Environment.NewLine}" +
        $"{Environment.NewLine}" +
        $"Would you like to upgrade?"
    ];

    internal static string[] msgbox_DatabaseUpdateSuccess() =>
    [
        $"Tingen Transmorger - Database update",
        $"Database upgraded successfully."
    ];
}