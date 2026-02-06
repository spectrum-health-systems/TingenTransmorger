namespace TingenTransmorger.Database;

/// <summary>
/// Builds a list of unique patient records from participant and message delivery data.
/// </summary>
/// <remarks>
/// This builder extracts patient information from the raw participant details and enriches
/// it with message delivery statistics. The result is a deduplicated list of patient
/// records suitable for inclusion in the transmorger database.
/// </remarks>
internal static class PatientsBuilder
{
    /// <summary>
    /// Constructs a list of patient records from the supplied input collections.
    /// </summary>
    /// <param name="tmpDir">
    /// Temporary directory path (reserved for future use if additional file reads are needed).
    /// </param>
    /// <param name="participantDetails">
    /// Optional list of participant detail dictionaries. Each entry should contain fields
    /// identifying patients (e.g., "ParticipantType", "ParticipantId", "Name", "Email").
    /// </param>
    /// <param name="messageDeliveryStats">
    /// Optional list of message delivery statistics. Used to enrich patient records with
    /// delivery success/failure counts.
    /// </param>
    /// <returns>
    /// A list of patient record dictionaries. Each dictionary represents a unique patient
    /// with standardized fields. Returns an empty list if no patient data is available.
    /// </returns>
    /// <remarks>
    /// Processing logic:
    /// - Filters participantDetails for entries where ParticipantType indicates a patient
    /// - Deduplicates by ParticipantId or Email
    /// - Joins with messageDeliveryStats to add delivery metrics
    /// - Creates a standardized patient record structure
    /// </remarks>
    public static List<Dictionary<string, object?>> Build(
        string tmpDir,
        List<Dictionary<string, object?>>? participantDetails,
        List<Dictionary<string, object?>>? messageDeliveryStats)
    {
        var patients = new List<Dictionary<string, object?>>();

        if (participantDetails == null || participantDetails.Count == 0)
        {
            return patients;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deliveryStatsMap = BuildDeliveryStatsMap(messageDeliveryStats);

        foreach (var participant in participantDetails)
        {
            // Filter for patients (adjust field names based on actual JSON structure)
            if (!IsPatient(participant))
            {
                continue;
            }

            var participantId = GetStringValue(participant, "ParticipantId")
                             ?? GetStringValue(participant, "PatientId")
                             ?? GetStringValue(participant, "Id");

            if (string.IsNullOrWhiteSpace(participantId) || !seenIds.Add(participantId))
            {
                continue; // Skip duplicates and entries without ID
            }

            var patientRecord = new Dictionary<string, object?>
            {
                ["PatientId"] = participantId,
                ["Name"] = GetStringValue(participant, "Name") ?? GetStringValue(participant, "ParticipantName"),
                ["Email"] = GetStringValue(participant, "Email") ?? GetStringValue(participant, "ParticipantEmail"),
                ["MeetingCount"] = GetIntValue(participant, "MeetingCount") ?? 0,
                ["ParticipantType"] = GetStringValue(participant, "ParticipantType") ?? "Patient"
            };

            // Add message delivery stats if available
            if (deliveryStatsMap.TryGetValue(participantId, out var stats))
            {
                patientRecord["MessagesSent"] = stats.Sent;
                patientRecord["MessagesDelivered"] = stats.Delivered;
                patientRecord["MessagesFailed"] = stats.Failed;
            }

            patients.Add(patientRecord);
        }

        return patients;
    }

    private static Dictionary<string, (int Sent, int Delivered, int Failed)> BuildDeliveryStatsMap(
        List<Dictionary<string, object?>>? messageDeliveryStats)
    {
        var map = new Dictionary<string, (int, int, int)>(StringComparer.OrdinalIgnoreCase);

        if (messageDeliveryStats == null)
        {
            return map;
        }

        foreach (var stat in messageDeliveryStats)
        {
            var participantId = GetStringValue(stat, "ParticipantId")
                             ?? GetStringValue(stat, "PatientId");

            if (string.IsNullOrWhiteSpace(participantId))
            {
                continue;
            }

            var sent = GetIntValue(stat, "MessagesSent") ?? GetIntValue(stat, "Sent") ?? 0;
            var delivered = GetIntValue(stat, "MessagesDelivered") ?? GetIntValue(stat, "Delivered") ?? 0;
            var failed = GetIntValue(stat, "MessagesFailed") ?? GetIntValue(stat, "Failed") ?? 0;

            map[participantId] = (sent, delivered, failed);
        }

        return map;
    }

    private static bool IsPatient(Dictionary<string, object?> participant)
    {
        var type = GetStringValue(participant, "ParticipantType")
                ?? GetStringValue(participant, "Type")
                ?? GetStringValue(participant, "Role");

        return type?.Equals("Patient", StringComparison.OrdinalIgnoreCase) == true
            || type?.Equals("Client", StringComparison.OrdinalIgnoreCase) == true
            || type?.Contains("Patient", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetStringValue(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    private static int? GetIntValue(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            if (value is int i)
                return i;
            if (value is long l)
                return (int)l;
            if (int.TryParse(value?.ToString(), out var result))
                return result;
        }
        return null;
    }
}
