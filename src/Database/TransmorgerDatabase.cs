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

    /// <summary>Builds the complete transmorger.json file from processed JSON reports.</summary>
    /// <param name="tmpDir">Directory containing processed JSON files.</param>
    internal static void Build(string tmpDir)
    {
        var database = new Dictionary<string, object?>
        {
            ["Summary"] = BuildSummaryComponent(tmpDir),
            ["Patients"] = BuildPatientsComponent(tmpDir),
            ["Providers"] = BuildProvidersComponent(tmpDir),
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
                    ["PhoneNumbers"] = new Dictionary<string, List<Dictionary<string, object?>>>(),
                    ["EmailAddresses"] = new Dictionary<string, List<Dictionary<string, object?>>>(),
                    ["Meetings"] = new List<Dictionary<string, object?>>()
                };
            }
        }

        // Step 2: Add phone numbers from SMS stats
        AddPhoneNumbersFromSmsStats(tmpDir, patientsByName);

        // Step 3: Add email addresses from Email stats
        AddEmailAddressesFromEmailStats(tmpDir, patientsByName);

        // Step 4: Add delivery success data from Message Delivery stats
        AddDeliverySuccessFromMessageDeliveryStats(tmpDir, patientsByName);

        // Step 5: Add email delivery success data from Message Delivery stats
        AddEmailDeliverySuccessFromMessageDeliveryStats(tmpDir, patientsByName);

        // Step 6: Add meetings from Participant Details
        AddMeetingsFromParticipantDetails(tmpDir, patientsByName);

        // Step 7: Convert phone number dictionary and email address dictionary to proper format
        return patientsByName.Values.Select(patient =>
        {
            var phoneNumbersDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumbers"]!;
            var deliverySuccessDict = patient.ContainsKey("PhoneNumberDeliverySuccess") 
                ? (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumberDeliverySuccess"]!
                : new Dictionary<string, List<Dictionary<string, object?>>>();

            var emailAddressesDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddresses"]!;
            var emailDeliverySuccessDict = patient.ContainsKey("EmailAddressDeliverySuccess")
                ? (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddressDeliverySuccess"]!
                : new Dictionary<string, List<Dictionary<string, object?>>>();

            return new Dictionary<string, object?>
            {
                ["PatientName"] = patient["PatientName"],
                ["PatientId"] = patient["PatientId"],
                ["PhoneNumbers"] = phoneNumbersDict.Select(kvp => new Dictionary<string, object?>
                {
                    ["Number"] = kvp.Key,
                    ["DeliveryFailure"] = kvp.Value,
                    ["DeliverySuccess"] = deliverySuccessDict.ContainsKey(kvp.Key) 
                        ? deliverySuccessDict[kvp.Key] 
                        : new List<Dictionary<string, object?>>()
                }).ToList(),
                ["EmailAddresses"] = emailAddressesDict.Select(kvp => new Dictionary<string, object?>
                {
                    ["Address"] = kvp.Key,
                    ["DeliveryFailure"] = kvp.Value,
                    ["DeliverySuccess"] = emailDeliverySuccessDict.ContainsKey(kvp.Key)
                        ? emailDeliverySuccessDict[kvp.Key]
                        : new List<Dictionary<string, object?>>()
                }).ToList(),
                ["Meetings"] = (List<Dictionary<string, object?>>)patient["Meetings"]!
            };
        }).ToList();
    }

    /// <summary>Builds the Providers component from Visit Details participant and meeting data.</summary>
    /// <param name="tmpDir">Directory containing source JSON files.</param>
    /// <returns>List of unique provider records.</returns>
    private static List<Dictionary<string, object?>> BuildProvidersComponent(string tmpDir)
    {
        var providersByName = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

        // Step 1: Build base provider records from participant details
        AddProvidersFromParticipantDetails(tmpDir, providersByName);

        // Step 2: Add provider IDs from meeting details
        AddProviderIdsFromMeetingDetails(tmpDir, providersByName);

        // Step 3: Convert to list and return
        return providersByName.Values.Select(provider => new Dictionary<string, object?>
        {
            ["ProviderName"] = provider["ProviderName"],
            ["ProviderId"] = provider["ProviderId"]
        }).ToList();
    }

    /// <summary>Adds provider names from Visit Details Participant Details.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="providersByName">Dictionary of providers keyed by name.</param>
    private static void AddProvidersFromParticipantDetails(string tmpDir, Dictionary<string, Dictionary<string, object?>> providersByName)
    {
        var participantDetails = ReadJsonFile(tmpDir, "Visit_Details-Participant_Details.json") as List<Dictionary<string, object?>>;

        if (participantDetails == null)
        {
            return;
        }

        foreach (var participant in participantDetails)
        {
            var participantType = GetStringValue(participant, "Participant Type");
            
            if (!participantType?.Equals("PROVIDER", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            var providerName = GetStringValue(participant, "Participant Name");

            if (string.IsNullOrWhiteSpace(providerName))
            {
                continue;
            }

            // Add provider if not already present
            if (!providersByName.ContainsKey(providerName))
            {
                providersByName[providerName] = new Dictionary<string, object?>
                {
                    ["ProviderName"] = providerName,
                    ["ProviderId"] = null  // Will be filled in from meeting details
                };
            }
        }
    }

    /// <summary>Adds provider IDs to provider records from Visit Details Meeting Details.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="providersByName">Dictionary of providers keyed by name.</param>
    private static void AddProviderIdsFromMeetingDetails(string tmpDir, Dictionary<string, Dictionary<string, object?>> providersByName)
    {
        var meetingDetails = ReadJsonFile(tmpDir, "Visit_Details-Meeting_Details.json") as List<Dictionary<string, object?>>;

        if (meetingDetails == null)
        {
            return;
        }

        var unmatchedProviders = new HashSet<string>();
        var debugInfo = new List<string>();

        foreach (var meeting in meetingDetails)
        {
            // Try different possible field name variations
            var providerNames = GetStringValue(meeting, "Provider/Staff Names") 
                             ?? GetStringValue(meeting, "Provider/Staff Name")
                             ?? GetStringValue(meeting, "Provider Names")
                             ?? GetStringValue(meeting, "Staff Names");
                             
            var providerIds = GetStringValue(meeting, "Provider IDs") 
                           ?? GetStringValue(meeting, "Provider ID")
                           ?? GetStringValue(meeting, "Staff IDs");

            if (string.IsNullOrWhiteSpace(providerNames) || string.IsNullOrWhiteSpace(providerIds))
            {
                continue;
            }

            debugInfo.Add($"Raw Provider Names: [{providerNames}]");
            debugInfo.Add($"Raw Provider IDs: [{providerIds}]");

            // Split provider names - use semicolon if present, otherwise assume single provider with comma in name
            var namesList = providerNames.Contains(';') 
                ? providerNames.Split(';').Select(n => n.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray()
                : new[] { providerNames.Trim() };
                
            var idsList = providerIds.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

            debugInfo.Add($"Split Names: [{string.Join(" | ", namesList)}]");
            debugInfo.Add($"Split IDs: [{string.Join(" | ", idsList)}]");

            // Match names with IDs (assuming they're in the same order)
            for (int i = 0; i < namesList.Length; i++)
            {
                var providerName = namesList[i];
                var providerId = (i < idsList.Length) ? idsList[i] : null;

                // Convert "Last, First" to "First Last" format to match Participant Details format
                var normalizedName = NormalizeProviderName(providerName);

                debugInfo.Add($"Original: [{providerName}] -> Normalized: [{normalizedName}] -> ID: [{providerId}]");

                if (providersByName.TryGetValue(normalizedName, out var provider))
                {
                    debugInfo.Add($"  MATCHED! Setting ID for [{normalizedName}]");
                    // Only set if not already set, or if this one is not null
                    if (provider["ProviderId"] == null && !string.IsNullOrWhiteSpace(providerId))
                    {
                        provider["ProviderId"] = providerId;
                    }
                }
                else
                {
                    debugInfo.Add($"  NOT MATCHED. Available providers: [{string.Join(", ", providersByName.Keys.Take(5))}...]");
                    // Provider from meeting details doesn't exist in participant details
                    if (!unmatchedProviders.Contains(normalizedName))
                    {
                        unmatchedProviders.Add($"{normalizedName} (original: {providerName})");
                    }
                }
            }
            debugInfo.Add("---");
        }

        if (debugInfo.Count > 0)
        {
            WriteErrorFile(tmpDir, "vdmd-debug.log", debugInfo);
        }

        if (unmatchedProviders.Count > 0)
        {
            WriteErrorFile(tmpDir, "vdmd-provider.error", unmatchedProviders.ToList());
        }
    }

    /// <summary>Normalizes provider name from "Last, First" to "First Last" format.</summary>
    /// <param name="name">Provider name that may be in "Last, First" format.</param>
    /// <returns>Normalized name in "First Last" format.</returns>
    private static string NormalizeProviderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Check if name contains a comma (indicating "Last, First" format)
        if (name.Contains(','))
        {
            var parts = name.Split(',').Select(p => p.Trim()).ToArray();
            if (parts.Length == 2)
            {
                // Convert "Last, First" to "First Last"
                return $"{parts[1]} {parts[0]}";
            }
        }

        // Return as-is if no comma found
        return name;
    }

    /// <summary>Adds phone numbers with failed meeting details to patient records from SMS stats.</summary>
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
                            var phoneNumbersDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumbers"]!;
                            
                            // Create failed meeting entry
                            var failedMeeting = new Dictionary<string, object?>
                            {
                                ["ErrorMessage"] = GetStringValue(recordDict, "Error Message"),
                                ["ScheduledStart"] = GetStringValue(recordDict, "Schedule Start Time"),
                                ["ScheduledEnd"] = GetStringValue(recordDict, "Schedule End Time"),
                                ["ProviderName"] = GetStringValue(recordDict, "Provider Name")
                            };

                            // Add to phone number's failed meetings list
                            if (!phoneNumbersDict.ContainsKey(sanitizedPhone))
                            {
                                phoneNumbersDict[sanitizedPhone] = new List<Dictionary<string, object?>>();
                            }
                            phoneNumbersDict[sanitizedPhone].Add(failedMeeting);
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

    /// <summary>Adds email addresses with failed meeting details to patient records from Email stats.</summary>
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
                            var emailAddressesDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddresses"]!;
                            
                            // Create failed meeting entry
                            var failedMeeting = new Dictionary<string, object?>
                            {
                                ["ErrorMessage"] = GetStringValue(recordDict, "Error Message"),
                                ["ScheduledStart"] = GetStringValue(recordDict, "Schedule Start Time"),
                                ["ScheduledEnd"] = GetStringValue(recordDict, "Schedule End Time"),
                                ["ProviderName"] = GetStringValue(recordDict, "Provider Name")
                            };

                            // Add to email address's failed meetings list
                            if (!emailAddressesDict.ContainsKey(emailAddress))
                            {
                                emailAddressesDict[emailAddress] = new List<Dictionary<string, object?>>();
                            }
                            emailAddressesDict[emailAddress].Add(failedMeeting);
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

    /// <summary>Adds delivery success data to phone numbers from Message Delivery Stats.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="patientsByName">Dictionary of patients keyed by name.</param>
    private static void AddDeliverySuccessFromMessageDeliveryStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName)
    {
        var deliveryStats = ReadJsonFile(tmpDir, "Message_Delivery-Message_Delivery_Stats.json") as List<Dictionary<string, object?>>;

        if (deliveryStats == null)
        {
            return;
        }

        // Iterate through all patients to build a phone number lookup
        var phoneToPatientMap = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var patient in patientsByName.Values)
        {
            var phoneNumbersDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumbers"]!;
            foreach (var phoneNumber in phoneNumbersDict.Keys)
            {
                if (!phoneToPatientMap.ContainsKey(phoneNumber))
                {
                    phoneToPatientMap[phoneNumber] = patient;
                }
            }
        }

        var unmatchedPhoneNumbers = new HashSet<string>();

        // Process each delivery record
        foreach (var record in deliveryStats)
        {
            var deliveryType = GetStringValue(record, "Delivery Type");
            
            // Only process SMS messages
            if (!deliveryType?.Equals("SMSMessage", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            var phoneNumber = GetStringValue(record, "Phone Number");
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                continue;
            }

            var sanitizedPhone = SanitizePhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(sanitizedPhone))
            {
                continue;
            }

            // Find the patient by phone number
            if (phoneToPatientMap.TryGetValue(sanitizedPhone, out var patient))
            {
                var phoneNumbersDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumbers"]!;
                
                // Ensure we have a DeliverySuccess list for this phone number
                if (!patient.ContainsKey("PhoneNumberDeliverySuccess"))
                {
                    patient["PhoneNumberDeliverySuccess"] = new Dictionary<string, List<Dictionary<string, object?>>>();
                }
                
                var deliverySuccessDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumberDeliverySuccess"]!;
                
                if (!deliverySuccessDict.ContainsKey(sanitizedPhone))
                {
                    deliverySuccessDict[sanitizedPhone] = new List<Dictionary<string, object?>>();
                }

                // Create delivery success entry
                var deliverySuccess = new Dictionary<string, object?>
                {
                    ["DeliveryStatus"] = GetStringValue(record, "Delivery Status"),
                    ["ErrorMessage"] = GetStringValue(record, "Error Message"),
                    ["MessageType"] = GetStringValue(record, "Message Type"),
                    ["DateSent"] = GetStringValue(record, "Date Sent"),
                    ["TimeSent"] = GetStringValue(record, "Time Sent")
                };

                deliverySuccessDict[sanitizedPhone].Add(deliverySuccess);
            }
            else
            {
                // Phone number doesn't match any patient
                if (!unmatchedPhoneNumbers.Contains(sanitizedPhone))
                {
                    unmatchedPhoneNumbers.Add(sanitizedPhone);
                }
            }
        }

        if (unmatchedPhoneNumbers.Count > 0)
        {
            WriteErrorFile(tmpDir, "mdmds-phone.error", unmatchedPhoneNumbers.ToList());
        }
    }

    /// <summary>Adds delivery success data to email addresses from Message Delivery Stats.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="patientsByName">Dictionary of patients keyed by name.</param>
    private static void AddEmailDeliverySuccessFromMessageDeliveryStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName)
    {
        var deliveryStats = ReadJsonFile(tmpDir, "Message_Delivery-Message_Delivery_Stats.json") as List<Dictionary<string, object?>>;

        if (deliveryStats == null)
        {
            return;
        }

        // Iterate through all patients to build an email address lookup
        var emailToPatientMap = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var patient in patientsByName.Values)
        {
            var emailAddressesDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddresses"]!;
            foreach (var emailAddress in emailAddressesDict.Keys)
            {
                if (!emailToPatientMap.ContainsKey(emailAddress))
                {
                    emailToPatientMap[emailAddress] = patient;
                }
            }
        }

        var unmatchedEmailAddresses = new HashSet<string>();

        // Process each delivery record
        foreach (var record in deliveryStats)
        {
            var deliveryType = GetStringValue(record, "Delivery Type");
            
            // Only process Email messages
            if (!deliveryType?.Equals("EmailMessage", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            var emailAddress = GetStringValue(record, "Email Address");
            
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                continue;
            }

            // Find the patient by email address
            if (emailToPatientMap.TryGetValue(emailAddress, out var patient))
            {
                var emailAddressesDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddresses"]!;
                
                // Ensure we have a DeliverySuccess list for this email address
                if (!patient.ContainsKey("EmailAddressDeliverySuccess"))
                {
                    patient["EmailAddressDeliverySuccess"] = new Dictionary<string, List<Dictionary<string, object?>>>();
                }
                
                var deliverySuccessDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddressDeliverySuccess"]!;
                
                if (!deliverySuccessDict.ContainsKey(emailAddress))
                {
                    deliverySuccessDict[emailAddress] = new List<Dictionary<string, object?>>();
                }

                // Create delivery success entry
                var deliverySuccess = new Dictionary<string, object?>
                {
                    ["DeliveryStatus"] = GetStringValue(record, "Delivery Status"),
                    ["ErrorMessage"] = GetStringValue(record, "Error Message"),
                    ["MessageType"] = GetStringValue(record, "Message Type"),
                    ["DateSent"] = GetStringValue(record, "Date Sent"),
                    ["TimeSent"] = GetStringValue(record, "Time Sent")
                };

                deliverySuccessDict[emailAddress].Add(deliverySuccess);
            }
            else
            {
                // Email address doesn't match any patient
                if (!unmatchedEmailAddresses.Contains(emailAddress))
                {
                    unmatchedEmailAddresses.Add(emailAddress);
                }
            }
        }

        if (unmatchedEmailAddresses.Count > 0)
        {
            WriteErrorFile(tmpDir, "mdmds-email.error", unmatchedEmailAddresses.ToList());
        }
    }

    /// <summary>Adds meeting information to patient records from Visit Details Participant Details.</summary>
    /// <param name="tmpDir">Directory containing JSON files.</param>
    /// <param name="patientsByName">Dictionary of patients keyed by name.</param>
    private static void AddMeetingsFromParticipantDetails(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName)
    {
        var participantDetails = ReadJsonFile(tmpDir, "Visit_Details-Participant_Details.json") as List<Dictionary<string, object?>>;

        if (participantDetails == null)
        {
            return;
        }

        var unmatchedPhoneNumbers = new HashSet<string>();
        var unmatchedPatientIds = new HashSet<string>();

        foreach (var participant in participantDetails)
        {
            if (!IsClient(participant))
            {
                continue;
            }

            var patientName = GetStringValue(participant, "Participant Name");
            var patientId = GetStringValue(participant, "MRN/PatientId");

            if (string.IsNullOrWhiteSpace(patientName))
            {
                continue;
            }

            // Check if patient exists
            if (!patientsByName.TryGetValue(patientName, out var patient))
            {
                // Patient doesn't exist - track error
                if (!string.IsNullOrWhiteSpace(patientId) && !unmatchedPatientIds.Contains(patientId))
                {
                    unmatchedPatientIds.Add(patientId);
                }
                continue;
            }

            var meetingId = GetStringValue(participant, "Meeting ID");
            if (string.IsNullOrWhiteSpace(meetingId))
            {
                continue;
            }

            // Build meeting record
            var meeting = new Dictionary<string, object?>
            {
                ["MeetingId"] = meetingId,
                ["Arrived"] = CombineDateAndTime(
                    GetStringValue(participant, "Participant Arrived Date"),
                    GetStringValue(participant, "Participant Arrived Time")
                ),
                ["Dropped"] = CombineDateAndTime(
                    GetStringValue(participant, "Participant Dropped Date"),
                    GetStringValue(participant, "Participant Dropped Time")
                ),
                ["Duration"] = GetStringValue(participant, "Participant Session Duration (minutes)"),
                ["CheckInViaChat"] = GetStringValue(participant, "Checked-in via Chat"),
                ["CheckInWait"] = GetStringValue(participant, "Check-in Waiting Time (Minutes)"),
                ["WaitForCareTeamMember"] = GetStringValue(participant, "Waiting For Care Team Member (Minutes)"),
                ["WaitForProvider"] = GetStringValue(participant, "Waiting For Provider (Minutes)"),
                ["CheckOutWait"] = GetStringValue(participant, "Check-out Waiting Time (Minutes)"),
                ["Rating"] = GetStringValue(participant, "Participant Rating"),
                ["Device"] = GetStringValue(participant, "Device Type"),
                ["Browser"] = CombineNameAndVersion(
                    GetStringValue(participant, "Browser Name"),
                    GetStringValue(participant, "Browser Version")
                ),
                ["Os"] = CombineNameAndVersion(
                    GetStringValue(participant, "Operating System Name"),
                    GetStringValue(participant, "Operating System Version")
                )
            };

            // Add meeting to patient's meetings list
            var meetings = (List<Dictionary<string, object?>>)patient["Meetings"]!;
            meetings.Add(meeting);
        }

        if (unmatchedPhoneNumbers.Count > 0)
        {
            WriteErrorFile(tmpDir, "vspd-phone.error", unmatchedPhoneNumbers.ToList());
        }

        if (unmatchedPatientIds.Count > 0)
        {
            WriteErrorFile(tmpDir, "vspd-patid.error", unmatchedPatientIds.ToList());
        }
    }

    /// <summary>Combines date and time strings into a single formatted string.</summary>
    /// <param name="date">Date string.</param>
    /// <param name="time">Time string.</param>
    /// <returns>Combined date and time string, or null if either is missing.</returns>
    private static string? CombineDateAndTime(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        return $"{date} {time}";
    }

    /// <summary>Combines name and version strings into a single formatted string.</summary>
    /// <param name="name">Name string.</param>
    /// <param name="version">Version string.</param>
    /// <returns>Combined name and version string, or null if either is missing.</returns>
    private static string? CombineNameAndVersion(string? name, string? version)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        return $"{name} {version}";
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

    /// <summary>Writes the complete database to transmorger.json.</summary>
    /// <param name="tmpDir">Output directory.</param>
    /// <param name="database">Complete database object to serialize.</param>
    private static void WriteDatabaseFile(string tmpDir, Dictionary<string, object?> database)
    {
        var path = Path.Combine(tmpDir, "transmorger.json");
        var json = JsonSerializer.Serialize(database, JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }
}