// 260205_code
// 260205_documentation

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TingenTransmorger.Database;

/// <summary>Builds the Transmorger database from processed report JSON files.</summary>
internal static class TransmorgerDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserves special characters like ' and -
    };

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    /// <summary>Builds the complete transmorger.db file from processed JSON reports.</summary>
    /// <param name="tmpDir">Directory containing processed JSON files.</param>
    internal static void Build(string tmpDir)
    {
        var database = new Dictionary<string, object?>
        {
            ["Summary"] = BuildSummaryComponent(tmpDir),
            ["Patients"] = BuildPatientsComponent(tmpDir),
            ["Providers"] = new List<object>(), // Placeholder for component 3
            ["Messaging"] = new List<object>(), // Placeholder for component 4
            ["Meetings"] = new List<object>() // Placeholder for component 5
        };

        WriteDatabaseFile(tmpDir, database);
    }

    /// <summary>Builds the Summary component containing Visit Stats and Message Failure summaries.</summary>
    /// <param name="tmpDir">Directory containing source JSON files.</param>
    /// <returns>Dictionary containing both summary sections.</returns>
    private static Dictionary<string, object?> BuildSummaryComponent(string tmpDir)
    {
        var visitStatsSummary = ReadJsonFile(tmpDir, "Visit_Stats-Summary.json");
        var messageFailureSummary = ReadJsonFile(tmpDir, "Message_Failure-Summary.json");

        return new Dictionary<string, object?>
        {
            ["VisitStats"] = visitStatsSummary,
            ["MessageFailure"] = messageFailureSummary
        };
    }

    /// <summary>Builds the Patients component from Visit Details participant data.</summary>
    /// <param name="tmpDir">Directory containing source JSON files.</param>
    /// <returns>List of unique patient records.</returns>
    private static List<Dictionary<string, object?>> BuildPatientsComponent(string tmpDir)
    {
        var participantDetails = ReadJsonFile(tmpDir, "Visit_Details-Participant_Details.json") as List<Dictionary<string, object?>>;

        if (participantDetails == null)
        {
            return new List<Dictionary<string, object?>>();
        }

        var patientsByName = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

        // Step 1: Build base patient records from participant details
        foreach (var participant in participantDetails)
        {
            if (!IsClient(participant))
            {
                continue;
            }

            var patientName = GetStringValue(participant, "Participant Name");
            var patientId = GetStringValue(participant, "MRN/PatientId");

            if (string.IsNullOrWhiteSpace(patientName) || string.IsNullOrWhiteSpace(patientId))
            {
                continue;
            }

            // Use PatientName as key for matching with Message Failure reports
            if (!patientsByName.ContainsKey(patientName))
            {
                patientsByName[patientName] = new Dictionary<string, object?>
                {
                    ["PatientName"] = patientName,
                    ["PatientId"] = patientId,
                    ["PhoneNumbers"] = new HashSet<string>(),
                    ["EmailAddresses"] = new HashSet<string>()
                };
            }
        }

        // Step 2: Add phone numbers from SMS stats
        AddPhoneNumbersFromSmsStats(tmpDir, patientsByName);

        // Step 3: Add email addresses from Email stats
        AddEmailAddressesFromEmailStats(tmpDir, patientsByName);

        // Step 4: Convert HashSets to Lists and return
        return patientsByName.Values.Select(patient => new Dictionary<string, object?>
        {
            ["PatientName"] = patient["PatientName"],
            ["PatientId"] = patient["PatientId"],
            ["PhoneNumbers"] = ((HashSet<string>)patient["PhoneNumbers"]!).ToList(),
            ["EmailAddresses"] = ((HashSet<string>)patient["EmailAddresses"]!).ToList()
        }).ToList();
    }

    /// <summary>Adds phone numbers to patient records from SMS stats.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="patientsByName">Dictionary of patients keyed by name.</param>
    private static void AddPhoneNumbersFromSmsStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName)
    {
        var smsStats = ReadJsonFile(tmpDir, "Message_Failure-Sms_Stats.json") as List<Dictionary<string, object?>>;

        if (smsStats == null)
        {
            return;
        }

        var missingPatients = new HashSet<string>();

        // Each entry has "Client Name" at parent level and "Records" array with phone numbers
        foreach (var clientEntry in smsStats)
        {
            var clientName = GetStringValue(clientEntry, "Client Name");

            if (string.IsNullOrWhiteSpace(clientName))
            {
                continue;
            }

            // Get the Records array
            if (clientEntry.TryGetValue("Records", out var recordsObj) && recordsObj is JsonElement recordsElement && recordsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var recordElement in recordsElement.EnumerateArray())
                {
                    var recordDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(recordElement.GetRawText());
                    if (recordDict == null)
                    {
                        continue;
                    }

                    var phoneNumber = GetStringValue(recordDict, "Phone Number");

                    if (string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        continue;
                    }

                    if (patientsByName.TryGetValue(clientName, out var patient))
                    {
                        var sanitizedPhone = SanitizePhoneNumber(phoneNumber);
                        if (!string.IsNullOrEmpty(sanitizedPhone))
                        {
                            ((HashSet<string>)patient["PhoneNumbers"]!).Add(sanitizedPhone);
                        }
                    }
                    else
                    {
                        if (!missingPatients.Contains(clientName))
                        {
                            missingPatients.Add(clientName);
                        }
                    }
                }
            }
        }

        if (missingPatients.Count > 0)
        {
            WriteErrorFile(tmpDir, "mfss-missing-patient.error", missingPatients.ToList());
        }
    }

    /// <summary>Adds email addresses to patient records from Email stats.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="patientsByName">Dictionary of patients keyed by name.</param>
    private static void AddEmailAddressesFromEmailStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName)
    {
        var emailStats = ReadJsonFile(tmpDir, "Message_Failure-Email_Stats.json") as List<Dictionary<string, object?>>;

        if (emailStats == null)
        {
            return;
        }

        var missingPatients = new List<string>();
        var invalidEmails = new List<string>();

        // Each entry has "Client Name" at parent level and "Records" array with email addresses
        foreach (var clientEntry in emailStats)
        {
            var clientName = GetStringValue(clientEntry, "Client Name");

            if (string.IsNullOrWhiteSpace(clientName))
            {
                continue;
            }

            // Get the Records array
            if (clientEntry.TryGetValue("Records", out var recordsObj) && recordsObj is JsonElement recordsElement && recordsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var recordElement in recordsElement.EnumerateArray())
                {
                    var recordDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(recordElement.GetRawText());
                    if (recordDict == null)
                    {
                        continue;
                    }

                    var emailAddress = GetStringValue(recordDict, "Email Address");

                    if (string.IsNullOrWhiteSpace(emailAddress))
                    {
                        continue;
                    }

                    if (patientsByName.TryGetValue(clientName, out var patient))
                    {
                        if (IsValidEmail(emailAddress))
                        {
                            ((HashSet<string>)patient["EmailAddresses"]!).Add(emailAddress);
                        }
                        else
                        {
                            invalidEmails.Add($"{clientName}: {emailAddress}");
                        }
                    }
                    else
                    {
                        if (!missingPatients.Contains(clientName))
                        {
                            missingPatients.Add(clientName);
                        }
                    }
                }
            }
        }

        if (missingPatients.Count > 0)
        {
            WriteErrorFile(tmpDir, "mfes-missing-patient.error", missingPatients);
        }

        if (invalidEmails.Count > 0)
        {
            WriteErrorFile(tmpDir, "mfes-invalid-email.error", invalidEmails);
        }
    }

    /// <summary>Sanitizes a phone number to 10-digit format (area code + local number).</summary>
    /// <param name="phoneNumber">Raw phone number string.</param>
    /// <returns>10-digit phone number, or empty string if invalid.</returns>
    private static string SanitizePhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Remove leading '1' if present (country code)
        if (digits.StartsWith("1") && digits.Length == 11)
        {
            digits = digits.Substring(1);
        }

        // Return only if we have exactly 10 digits
        return digits.Length == 10 ? digits : string.Empty;
    }

    /// <summary>Validates email address format.</summary>
    /// <param name="email">Email address to validate.</param>
    /// <returns>True if email follows standard format.</returns>
    private static bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }

    /// <summary>Writes error messages to a file.</summary>
    /// <param name="tmpDir">Directory to write the error file.</param>
    /// <param name="fileName">Name of the error file.</param>
    /// <param name="errors">List of error messages.</param>
    private static void WriteErrorFile(string tmpDir, string fileName, List<string> errors)
    {
        var filePath = Path.Combine(tmpDir, fileName);
        File.WriteAllLines(filePath, errors, Encoding.UTF8);
    }

    /// <summary>Checks if a participant record represents a client (patient).</summary>
    /// <param name="participant">Participant record dictionary.</param>
    /// <returns>True if the participant is a CLIENT.</returns>
    private static bool IsClient(Dictionary<string, object?> participant)
    {
        var participantType = GetStringValue(participant, "Participant Type");
        return participantType?.Equals("CLIENT", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>Safely extracts a string value from a dictionary.</summary>
    /// <param name="dict">Source dictionary.</param>
    /// <param name="key">Key to look up.</param>
    /// <returns>Trimmed string value, or null if not found.</returns>
    private static string? GetStringValue(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString()?.Trim();
        }
        return null;
    }

    /// <summary>Reads and deserializes a JSON file.</summary>
    /// <param name="tmpDir">Directory containing the file.</param>
    /// <param name="fileName">Name of the JSON file to read.</param>
    /// <returns>Deserialized object, or empty list if file doesn't exist.</returns>
    private static object? ReadJsonFile(string tmpDir, string fileName)
    {
        var filePath = Path.Combine(tmpDir, fileName);

        if (!File.Exists(filePath))
        {
            return new List<object>();
        }

        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json);
    }

    /// <summary>Writes the complete database to transmorger.db.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="database">Complete database object to serialize.</param>
    private static void WriteDatabaseFile(string tmpDir, Dictionary<string, object?> database)
    {
        var path = Path.Combine(tmpDir, "transmorger.db");
        var json = JsonSerializer.Serialize(database, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }
}