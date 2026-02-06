// 260205_code
// 260205_documentation

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TingenTransmorger.Database;

/// <summary>Builds the Transmorger database from processed report JSON files.</summary>
public class TransmorgerDatabase
{


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserves special characters like ' and -
    };

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    // Holds the parsed database JSON when loaded via Load()
    private JsonElement _jsonRoot;
    private bool _hasData;

    internal static TransmorgerDatabase Load(string localDb)
    {
        // If no path supplied, default to "Database/transmorger.db" under app base
        if (string.IsNullOrWhiteSpace(localDb))
        {
            localDb = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), "Database", "transmorger.db");
        }



        // Resolve path: try as provided, then relative to application base directory
        var path = localDb;
        if (!File.Exists(path))
        {
            var alt = Path.Combine(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory(), localDb);
            if (File.Exists(alt))
            {
                path = alt;
            }
            else
            {
                throw new FileNotFoundException($"Database file not found: {localDb}", localDb);
            }
        }

        var json = File.ReadAllText(path, Encoding.UTF8);

        using var doc = JsonDocument.Parse(json);
        var instance = new TransmorgerDatabase();
        // Clone the root element so it lives beyond the JsonDocument scope
        instance._jsonRoot = doc.RootElement.Clone();
        instance._hasData = true;

        return instance;
    }

    /// <summary>Returns the VisitStats section from the loaded database as pretty JSON.</summary>
    public string GetSummaryVisitStatsJson()
    {
        if (!_hasData)
            return string.Empty;
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
            return string.Empty;
        if (!summary.TryGetProperty("VisitStats", out var visit))
            return string.Empty; // Check for VisitStats
        return JsonSerializer.Serialize(visit, JsonOptions);
    }

    /// <summary>Returns the MessageFailure section from the loaded database as pretty JSON.</summary>
    public string GetSummaryMessageFailureJson()
    {
        if (!_hasData)
            return string.Empty;
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
            return string.Empty;
        if (!summary.TryGetProperty("MessageFailure", out var mf))
            return string.Empty; // Check for MessageFailure
        return JsonSerializer.Serialize(mf, JsonOptions);
    }

    /// <summary>Builds the complete transmorger.json file from processed JSON reports.</summary>
    /// <param name="tmpDir">Directory containing processed JSON files.</param>
    internal static void Build(string tmpDir, string masterDbDir)
    {
        // Cache all file reads at the top level to avoid redundant I/O operations
        var participantDetails   = ReadJsonFile(tmpDir, "Visit_Details-Participant_Details.json") as List<Dictionary<string, object?>>;
        var meetingDetails       = ReadJsonFile(tmpDir, "Visit_Details-Meeting_Details.json") as List<Dictionary<string, object?>>;
        var messageDeliveryStats = ReadJsonFile(tmpDir, "Message_Delivery-Message_Delivery_Stats.json") as List<Dictionary<string, object?>>;
        var patients             = BuildPatientsComponent(tmpDir, participantDetails, messageDeliveryStats);
        var providers            = BuildProvidersComponent(tmpDir, participantDetails, meetingDetails);

        var database = new Dictionary<string, object?>
        {
            ["Summary"]       = BuildSummaryComponent(tmpDir),
            ["Patients"]      = patients,
            ["Providers"]     = providers,
            ["MeetingDetail"] = BuildMeetingDetailComponent(tmpDir, meetingDetails, patients, providers),
            ["MeetingError"]  = BuildMeetingErrorComponent(tmpDir, patients, providers),
        };

        WriteDatabaseFile(tmpDir, masterDbDir, database);
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
    /// <param name="participantDetails">Cached participant details data.</param>
    /// <param name="messageDeliveryStats">Cached message delivery stats data.</param>
    /// <returns>List of unique patient records.</returns>
    private static List<Dictionary<string, object?>> BuildPatientsComponent(string tmpDir, List<Dictionary<string, object?>>? participantDetails, List<Dictionary<string, object?>>? messageDeliveryStats)
    {
        if (participantDetails == null)
        {
            return new List<Dictionary<string, object?>>();
        }

        var patientsByName = new Dictionary<string, Dictionary<string, object?>>(participantDetails.Count, StringComparer.OrdinalIgnoreCase);

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
                patientsByName[patientName] = new Dictionary<string, object?>(8)
                {
                    ["PatientName"] = patientName,
                    ["PatientId"] = patientId,
                    ["PhoneNumbers"] = new Dictionary<string, List<Dictionary<string, object?>>>(),
                    ["EmailAddresses"] = new Dictionary<string, List<Dictionary<string, object?>>>(),
                    ["Meetings"] = new List<Dictionary<string, object?>>()
                };
            }
        }

        AddPhoneNumbersFromSmsStats(tmpDir, patientsByName);

        AddEmailAddressesFromEmailStats(tmpDir, patientsByName);

        AddDeliverySuccessFromMessageDeliveryStats(tmpDir, patientsByName, messageDeliveryStats);

        AddEmailDeliverySuccessFromMessageDeliveryStats(tmpDir, patientsByName, messageDeliveryStats);

        AddMeetingsFromParticipantDetails(tmpDir, patientsByName, participantDetails);

        var result = new List<Dictionary<string, object?>>(patientsByName.Count);
        foreach (var patient in patientsByName.Values)
        {
            var phoneNumbersDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["PhoneNumbers"]!;
            var deliverySuccessDict = patient.TryGetValue("PhoneNumberDeliverySuccess", out var pnds)
                ? (Dictionary<string, List<Dictionary<string, object?>>>)pnds!
                : new Dictionary<string, List<Dictionary<string, object?>>>(0);

            var emailAddressesDict = (Dictionary<string, List<Dictionary<string, object?>>>)patient["EmailAddresses"]!;
            var emailDeliverySuccessDict = patient.TryGetValue("EmailAddressDeliverySuccess", out var eads)
                ? (Dictionary<string, List<Dictionary<string, object?>>>)eads!
                : new Dictionary<string, List<Dictionary<string, object?>>>(0);

            var patientResult = new Dictionary<string, object?>(6)
            {
                ["PatientName"] = patient["PatientName"],
                ["PatientId"] = patient["PatientId"],
                ["PhoneNumbers"] = phoneNumbersDict.Select(kvp => new Dictionary<string, object?>(3)
                {
                    ["Number"] = kvp.Key,
                    ["DeliveryFailure"] = kvp.Value,
                    ["DeliverySuccess"] = deliverySuccessDict.TryGetValue(kvp.Key, out var successList)
                        ? successList
                        : new List<Dictionary<string, object?>>(0)
                }).ToList(),
                ["EmailAddresses"] = emailAddressesDict.Select(kvp => new Dictionary<string, object?>(3)
                {
                    ["Address"] = kvp.Key,
                    ["DeliveryFailure"] = kvp.Value,
                    ["DeliverySuccess"] = emailDeliverySuccessDict.TryGetValue(kvp.Key, out var successList)
                        ? successList
                        : new List<Dictionary<string, object?>>(0)
                }).ToList(),
                ["Meetings"] = (List<Dictionary<string, object?>>)patient["Meetings"]!
            };
            result.Add(patientResult);
        }

        return result;
    }

    /// <summary>Builds the Providers component from Visit Details participant and meeting data.</summary>
    /// <param name="tmpDir">Directory for error file output.</param>
    /// <param name="participantDetails">Cached participant details data.</param>
    /// <param name="meetingDetails">Cached meeting details data.</param>
    /// <returns>List of unique provider records.</returns>
    private static List<Dictionary<string, object?>> BuildProvidersComponent(string tmpDir, List<Dictionary<string, object?>>? participantDetails, List<Dictionary<string, object?>>? meetingDetails)
    {
        var estimatedSize = (participantDetails?.Count ?? 0) + (meetingDetails?.Count ?? 0);
        var providersByName = new Dictionary<string, Dictionary<string, object?>>(estimatedSize / 4, StringComparer.OrdinalIgnoreCase);

        AddProvidersFromParticipantDetails(providersByName, participantDetails);

        AddProviderDataFromMeetingDetails(tmpDir, providersByName, meetingDetails);

        // Step 4: Convert to list and return
        var result = new List<Dictionary<string, object?>>(providersByName.Count);
        foreach (var provider in providersByName.Values)
        {
            result.Add(new Dictionary<string, object?>(3)
            {
                ["ProviderName"] = provider["ProviderName"],
                ["ProviderId"] = provider["ProviderId"],
                ["Meetings"] = (List<string>)provider["Meetings"]!
            });
        }
        return result;
    }

    /// <summary>Builds the MeetingDetail component from Visit Details Meeting Details.</summary>
    /// <param name="tmpDir">Directory for error file output.</param>
    /// <param name="meetingDetails">Cached meeting details data.</param>
    /// <param name="patients">List of patient records for validation.</param>
    /// <param name="providers">List of provider records for validation.</param>
    /// <returns>Dictionary of meeting details indexed by MeetingId.</returns>
    private static Dictionary<string, object?> BuildMeetingDetailComponent(string tmpDir, List<Dictionary<string, object?>>? meetingDetails, List<Dictionary<string, object?>> patients, List<Dictionary<string, object?>> providers)
    {
        if (meetingDetails == null)
        {
            return new Dictionary<string, object?>(0);
        }

        var meetingDetailDict = new Dictionary<string, object?>(meetingDetails.Count, StringComparer.OrdinalIgnoreCase);
        var validationErrors = new List<string>();

        // Build lookup sets for validation
        var patientNames = new HashSet<string>(patients.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in patients)
        {
            var name = GetStringValue(p, "PatientName");
            if (name != null) patientNames.Add(name);
        }

        var providerNames = new HashSet<string>(providers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in providers)
        {
            var name = GetStringValue(p, "ProviderName");
            if (name != null) providerNames.Add(name);
        }

        foreach (var meeting in meetingDetails)
        {
            var meetingId = GetStringValue(meeting, "Meeting ID") ?? GetStringValue(meeting, "MeetingId");

            if (string.IsNullOrWhiteSpace(meetingId))
            {
                continue;
            }

            // Get provider and participant names for validation
            var providerNamesField = GetStringValue(meeting, "Provider/Staff Names")
                                  ?? GetStringValue(meeting, "Provider/Staff Name")
                                  ?? GetStringValue(meeting, "Provider Names")
                                  ?? GetStringValue(meeting, "Staff Names");

            var participantNamesField = GetStringValue(meeting, "Participant Names");

            // Validate provider names
            if (!string.IsNullOrWhiteSpace(providerNamesField))
            {
                char delimiter = providerNamesField.Contains(';') ? ';' : ',';
                var providerNamesList = providerNamesField.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var providerName in providerNamesList)
                {
                    if (!TryMatchProviderName(providerName, providerNames, out _))
                    {
                        validationErrors.Add($"MeetingId: {meetingId} | Provider not found: {providerName}");
                    }
                }
            }

            // Validate participant/patient names
            if (!string.IsNullOrWhiteSpace(participantNamesField))
            {
                // Participant Names might contain multiple names separated by commas or semicolons
                char delimiter = participantNamesField.Contains(';') ? ';' : ',';
                var participantNamesList = participantNamesField.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var participantName in participantNamesList)
                {
                    if (!patientNames.Contains(participantName))
                    {
                        validationErrors.Add($"MeetingId: {meetingId} | Patient not found: {participantName}");
                    }
                }
            }

            // Create meeting detail entry
            var meetingDetail = new Dictionary<string, object?>(20)
            {
                ["AppointmentId"] = GetStringValue(meeting, "Appointment ID"),
                ["MeetingTitle"] = GetStringValue(meeting, "Meeting Title"),
                ["Workflow"] = GetStringValue(meeting, "Workflow"),
                ["Program"] = GetStringValue(meeting, "Program"),
                ["ServiceCode"] = GetStringValue(meeting, "Service Code"),
                ["ScheduledStart"] = CombineDateAndTime(
                    GetStringValue(meeting, "Scheduled Start Date"),
                    GetStringValue(meeting, "Scheduled Start Time")
                ),
                ["ScheduledEnd"] = CombineDateAndTime(
                    GetStringValue(meeting, "Scheduled End Date"),
                    GetStringValue(meeting, "Scheduled End Time")
                ),
                ["ActualStart"] = CombineDateAndTime(
                    GetStringValue(meeting, "Actual Start Date"),
                    GetStringValue(meeting, "Actual Start Time")
                ),
                ["ActualEnd"] = CombineDateAndTime(
                    GetStringValue(meeting, "Actual End Date"),
                    GetStringValue(meeting, "Actual End Time")
                ),
                ["Duration"] = GetStringValue(meeting, "Duration (Minutes)"),
                ["Joins"] = GetStringValue(meeting, "Meeting Joins"),
                ["Status"] = GetStringValue(meeting, "Meeting Status"),
                ["CheckedInByFrontDesk"] = GetStringValue(meeting, "Checked-in by Front Desk"),
                ["EndedBy"] = GetStringValue(meeting, "Meeting Ended By"),
                ["InitiatedBy"] = GetStringValue(meeting, "Meeting Initiated By"),
                ["ScribeEnabled"] = GetStringValue(meeting, "Scribe Enabled"),
                ["ScribeConsentAcceptance"] = GetStringValue(meeting, "Scribe Consent Acceptance"),
                ["ParticipantNames"] = participantNamesField,
                ["ProviderNames"] = providerNamesField,
                ["ProviderIds"] = GetStringValue(meeting, "Provider IDs")
                               ?? GetStringValue(meeting, "Provider ID")
                               ?? GetStringValue(meeting, "Staff IDs")
            };

            // Add to dictionary using MeetingId as key
            meetingDetailDict[meetingId] = meetingDetail;
        }

        // Write validation errors if any
        if (validationErrors.Count > 0)
        {
            WriteErrorFile(tmpDir, "sdmd-misc.error", validationErrors);
        }

        return meetingDetailDict;
    }

    /// <summary>Builds the MeetingError component from Visit Stats Meeting Errors.</summary>
    /// <param name="tmpDir">Directory containing source JSON files.</param>
    /// <param name="patients">List of patient records for validation.</param>
    /// <param name="providers">List of provider records for validation.</param>
    /// <returns>Dictionary of meeting errors indexed by MeetingId.</returns>
    private static Dictionary<string, object?> BuildMeetingErrorComponent(string tmpDir, List<Dictionary<string, object?>> patients, List<Dictionary<string, object?>> providers)
    {
        var meetingErrors = ReadJsonFile(tmpDir, "Visit_Stats-Meeting_Errors.json") as List<Dictionary<string, object?>>;

        if (meetingErrors == null)
        {
            return new Dictionary<string, object?>(0);
        }

        var meetingErrorDict = new Dictionary<string, object?>(meetingErrors.Count, StringComparer.OrdinalIgnoreCase);
        var validationErrors = new List<string>();

        // Build lookup sets for validation
        var patientNames = new HashSet<string>(patients.Count, StringComparer.OrdinalIgnoreCase);
        var patientIds = new HashSet<string>(patients.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in patients)
        {
            var name = GetStringValue(p, "PatientName");
            if (name != null) patientNames.Add(name);
            var id = GetStringValue(p, "PatientId");
            if (id != null) patientIds.Add(id);
        }

        var providerNames = new HashSet<string>(providers.Count, StringComparer.OrdinalIgnoreCase);
        var providerIds = new HashSet<string>(providers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in providers)
        {
            var name = GetStringValue(p, "ProviderName");
            if (name != null) providerNames.Add(name);
            var id = GetStringValue(p, "ProviderId");
            if (!string.IsNullOrWhiteSpace(id)) providerIds.Add(id);
        }

        foreach (var error in meetingErrors)
        {
            var meetingId = GetStringValue(error, "Meeting ID") ?? GetStringValue(error, "MeetingId");

            if (string.IsNullOrWhiteSpace(meetingId))
            {
                continue;
            }

            // Get fields for validation
            var patientName = GetStringValue(error, "Client Name");
            var patientId = GetStringValue(error, "Client MRN/Patient ID");
            var providerNamesField = GetStringValue(error, "Provider Names");
            var providerIdField = GetStringValue(error, "Provider Identifiers");

            // Validate patient name
            if (!string.IsNullOrWhiteSpace(patientName) && !patientNames.Contains(patientName))
            {
                validationErrors.Add($"MeetingId: {meetingId} | PatientName not found: {patientName}");
            }

            // Validate patient ID
            if (!string.IsNullOrWhiteSpace(patientId) && !patientIds.Contains(patientId))
            {
                validationErrors.Add($"MeetingId: {meetingId} | PatientId not found: {patientId}");
            }

            // Validate provider names
            if (!string.IsNullOrWhiteSpace(providerNamesField))
            {
                char delimiter = providerNamesField.Contains(';') ? ';' : ',';
                var providerNamesList = providerNamesField.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var providerName in providerNamesList)
                {
                    if (!TryMatchProviderName(providerName, providerNames, out _))
                    {
                        validationErrors.Add($"MeetingId: {meetingId} | ProviderName not found: {providerName}");
                    }
                }
            }

            // Validate provider IDs
            if (!string.IsNullOrWhiteSpace(providerIdField))
            {
                var providerIdsList = providerIdField.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var providerId in providerIdsList)
                {
                    if (!providerIds.Contains(providerId))
                    {
                        validationErrors.Add($"MeetingId: {meetingId} | ProviderId not found: {providerId}");
                    }
                }
            }

            // Create meeting error entry
            var meetingError = new Dictionary<string, object?>(10)
            {
                ["AttendeeId"] = GetStringValue(error, "Attendee ID"),
                ["AttendeeType"] = GetStringValue(error, "Attendee Type"),
                ["PatientName"] = patientName,
                ["PatientId"] = patientId,
                ["ProviderNames"] = providerNamesField,
                ["ProviderId"] = providerIdField,
                ["Duration"] = GetStringValue(error, "Duration"),
                ["Browser"] = CombineNameAndVersion(
                    GetStringValue(error, "Browser Name"),
                    GetStringValue(error, "Browser Version")
                ),
                ["Os"] = CombineNameAndVersion(
                    GetStringValue(error, "OS Name"),
                    GetStringValue(error, "OS Version")
                ),
                ["Kind"] = GetStringValue(error, "Kind"),
                ["Reason"] = GetStringValue(error, "Reason")
            };

            // Add to dictionary using MeetingId as key
            // If multiple errors exist for the same meeting, this will keep the last one
            // If you want to keep all errors, you'd need to use a List<Dictionary<string, object?>> instead
            meetingErrorDict[meetingId] = meetingError;
        }

        // Write validation errors if any
        if (validationErrors.Count > 0)
        {
            WriteErrorFile(tmpDir, "vsme-misc.error", validationErrors);
        }

        return meetingErrorDict;
    }

    /// <summary>Adds provider names from Visit Details Participant Details.</summary>
    /// <param name="providersByName">Dictionary of providers keyed by name.</param>
    /// <param name="participantDetails">Cached participant details data.</param>
    private static void AddProvidersFromParticipantDetails(Dictionary<string, Dictionary<string, object?>> providersByName, List<Dictionary<string, object?>>? participantDetails)
    {
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
                    ["ProviderId"] = null,  // Will be filled in from meeting details
                    ["Meetings"] = new List<string>()  // Will be filled in from meeting details
                };
            }
        }
    }

    /// <summary>Consolidates provider IDs and meeting IDs from Visit Details Meeting Details in a single pass.</summary>
    /// <param name="tmpDir">Directory for error file output.</param>
    /// <param name="providersByName">Dictionary of providers keyed by name.</param>
    /// <param name="meetingDetails">Cached meeting details data.</param>
    private static void AddProviderDataFromMeetingDetails(string tmpDir, Dictionary<string, Dictionary<string, object?>> providersByName, List<Dictionary<string, object?>>? meetingDetails)
    {
        if (meetingDetails == null)
        {
            return;
        }

        var unmatchedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmatchedMeetingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var debugInfo = new List<string>(meetingDetails.Count * 4);

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

            // Split provider names - use semicolon first, then comma as fallback delimiter
            char delimiter = providerNames.Contains(';') ? ';' : ',';
            var namesList = providerNames.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var idsList = providerIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            debugInfo.Add($"Split Names: [{string.Join(" | ", namesList)}]");
            debugInfo.Add($"Split IDs: [{string.Join(" | ", idsList)}]");

            var meetingId = GetStringValue(meeting, "Meeting ID") ?? GetStringValue(meeting, "MeetingId");
            bool meetingMatched = false;

            // Match names with IDs (assuming they're in the same order)
            for (int i = 0; i < namesList.Length; i++)
            {
                var providerName = namesList[i];
                var providerId = (i < idsList.Length) ? idsList[i] : null;

                if (TryMatchProviderInDictionary(providerName, providersByName, out var provider, out var matchedName))
                {
                    debugInfo.Add($"Original: [{providerName}] -> Matched: [{matchedName}] -> ID: [{providerId}]");
                    debugInfo.Add($"  MATCHED! Setting ID for [{matchedName}]");

                    // Set provider ID if not already set
                    if (provider!["ProviderId"] == null && !string.IsNullOrWhiteSpace(providerId))
                    {
                        provider["ProviderId"] = providerId;
                    }

                    // Add meeting ID if present and not already added
                    if (!string.IsNullOrWhiteSpace(meetingId))
                    {
                        var meetings = (List<string>)provider["Meetings"]!;
                        if (!meetings.Contains(meetingId))
                        {
                            meetings.Add(meetingId);
                        }
                        meetingMatched = true;
                    }
                }
                else
                {
                    debugInfo.Add($"  NOT MATCHED. Available providers: [{string.Join(", ", providersByName.Keys.Take(5))}...]");
                    unmatchedProviders.Add($"{providerName} (tried all formats)");
                }
            }

            // Track unmatched meeting IDs
            if (!string.IsNullOrWhiteSpace(meetingId) && !meetingMatched)
            {
                unmatchedMeetingIds.Add(meetingId);
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

        if (unmatchedMeetingIds.Count > 0)
        {
            WriteErrorFile(tmpDir, "vsmd-meetingid.error", unmatchedMeetingIds.ToList());
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

        // Check if name contains ", " (comma followed by space - indicating "Last, First" format)
        if (name.Contains(", "))
        {
            var parts = name.Split(',').Select(p => p.Trim()).ToArray();
            if (parts.Length == 2)
            {
                // Convert "Last, First" to "First Last"
                return $"{parts[1]} {parts[0]}";
            }
        }

        // Return as-is if no comma-space pattern found
        return name;
    }

    /// <summary>Reverses space-separated name parts to handle "LAST FIRST" to "FIRST LAST" conversion.</summary>
    /// <param name="name">Provider name in "LAST FIRST" format.</param>
    /// <returns>Reversed name in "FIRST LAST" format.</returns>
    private static string ReverseNameParts(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Split on space and reverse the parts
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return $"{parts[1]} {parts[0]}";
        }

        // Return as-is if not exactly two parts
        return name;
    }

    /// <summary>Tries to match a provider name in various formats against a HashSet.</summary>
    /// <param name="providerName">Provider name to match.</param>
    /// <param name="providerNames">HashSet of known provider names.</param>
    /// <param name="matchedName">The matched name if found.</param>
    /// <returns>True if a match was found.</returns>
    private static bool TryMatchProviderName(string providerName, HashSet<string> providerNames, out string? matchedName)
    {
        matchedName = null;

        // Try as-is
        if (providerNames.Contains(providerName))
        {
            matchedName = providerName;
            return true;
        }

        // Try reversed
        var reversed = ReverseNameParts(providerName);
        if (providerNames.Contains(reversed))
        {
            matchedName = reversed;
            return true;
        }

        // Try normalized
        var normalized = NormalizeProviderName(providerName);
        if (providerNames.Contains(normalized))
        {
            matchedName = normalized;
            return true;
        }

        return false;
    }

    /// <summary>Tries to match a provider name in various formats against a Dictionary.</summary>
    /// <param name="providerName">Provider name to match.</param>
    /// <param name="providersByName">Dictionary of providers keyed by name.</param>
    /// <param name="provider">The matched provider dictionary if found.</param>
    /// <param name="matchedName">The matched name if found.</param>
    /// <returns>True if a match was found.</returns>
    private static bool TryMatchProviderInDictionary(string providerName, Dictionary<string, Dictionary<string, object?>> providersByName, out Dictionary<string, object?>? provider, out string? matchedName)
    {
        provider = null;
        matchedName = null;

        // Try as-is
        if (providersByName.TryGetValue(providerName, out provider))
        {
            matchedName = providerName;
            return true;
        }

        // Try reversed
        var reversed = ReverseNameParts(providerName);
        if (providersByName.TryGetValue(reversed, out provider))
        {
            matchedName = reversed;
            return true;
        }

        // Try normalized
        var normalized = NormalizeProviderName(providerName);
        if (providersByName.TryGetValue(normalized, out provider))
        {
            matchedName = normalized;
            return true;
        }

        return false;
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
    /// <param name="messageDeliveryStats">Cached message delivery stats data.</param>
    private static void AddDeliverySuccessFromMessageDeliveryStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName, List<Dictionary<string, object?>>? messageDeliveryStats)
    {
        if (messageDeliveryStats == null)
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
        foreach (var record in messageDeliveryStats)
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
    /// <param name="messageDeliveryStats">Cached message delivery stats data.</param>
    private static void AddEmailDeliverySuccessFromMessageDeliveryStats(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName, List<Dictionary<string, object?>>? messageDeliveryStats)
    {
        if (messageDeliveryStats == null)
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
        foreach (var record in messageDeliveryStats)
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
    /// <param name="participantDetails">Cached participant details data.</param>
    private static void AddMeetingsFromParticipantDetails(string tmpDir, Dictionary<string, Dictionary<string, object?>> patientsByName, List<Dictionary<string, object?>>? participantDetails)
    {
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
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Use Span to avoid allocations
        Span<char> buffer = stackalloc char[phoneNumber.Length];
        int digitCount = 0;

        foreach (char c in phoneNumber)
        {
            if (char.IsDigit(c))
            {
                buffer[digitCount++] = c;
            }
        }

        // Remove leading '1' if present (country code)
        if (digitCount == 11 && buffer[0] == '1')
        {
            return new string(buffer.Slice(1, 10));
        }

        // Return only if we have exactly 10 digits
        return digitCount == 10 ? new string(buffer.Slice(0, 10)) : string.Empty;
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
        if (dict.TryGetValue(key, out var value))
        {
            if (value is string str)
            {
                return str.Trim();
            }
            else if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            {
                return jsonElement.GetString()?.Trim();
            }
            else if (value != null)
            {
                return value.ToString()?.Trim();
            }
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
    private static void WriteDatabaseFile(string tmpDir, string masterDbDir, Dictionary<string, object?> database)
    {
        var jsonPath = Path.Combine(tmpDir, "transmorger.json");
        var json = JsonSerializer.Serialize(database, JsonOptions);
        File.WriteAllText(jsonPath, json, Encoding.UTF8);

        var dbTempPath = Path.Combine(tmpDir, "transmorger.db");
        var db = JsonSerializer.Serialize(database);
        File.WriteAllText(dbTempPath, db, Encoding.UTF8);

        var masterDbPath = Path.Combine(masterDbDir, "transmorger.db");
        File.Copy(dbTempPath, masterDbPath);
    }

}
