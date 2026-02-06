namespace TingenTransmorger.Database;

/// <summary>
/// Builds a list of unique provider records from participant and meeting data.
/// </summary>
/// <remarks>
/// This builder extracts provider information from raw participant details and enriches
/// it with meeting statistics. The result is a deduplicated list of provider records
/// suitable for inclusion in the transmorger database.
/// </remarks>
internal static class ProvidersBuilder
{
    /// <summary>
    /// Constructs a list of provider records from the supplied input collections.
    /// </summary>
    /// <param name="tmpDir">
    /// Temporary directory path (reserved for future use if additional file reads are needed).
    /// </param>
    /// <param name="participantDetails">
    /// Optional list of participant detail dictionaries. Each entry should contain fields
    /// identifying providers (e.g., "ParticipantType", "ProviderId", "Name", "Email").
    /// </param>
    /// <param name="meetingDetails">
    /// Optional list of meeting detail dictionaries. Used to enrich provider records with
    /// meeting counts and statistics.
    /// </param>
    /// <returns>
    /// A list of provider record dictionaries. Each dictionary represents a unique provider
    /// with standardized fields. Returns an empty list if no provider data is available.
    /// </returns>
    /// <remarks>
    /// Processing logic:
    /// - Filters participantDetails for entries where ParticipantType indicates a provider
    /// - Deduplicates by ProviderId or Email
    /// - Joins with meetingDetails to add meeting metrics
    /// - Creates a standardized provider record structure
    /// </remarks>
    public static List<Dictionary<string, object?>> Build(
        string tmpDir,
        List<Dictionary<string, object?>>? participantDetails,
        List<Dictionary<string, object?>>? meetingDetails)
    {
        var providers = new List<Dictionary<string, object?>>();

        if (participantDetails == null || participantDetails.Count == 0)
        {
            return providers;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var meetingCountMap = BuildMeetingCountMap(meetingDetails);

        foreach (var participant in participantDetails)
        {
            // Filter for providers
            if (!IsProvider(participant))
            {
                continue;
            }

            var providerId = GetStringValue(participant, "ProviderId")
                          ?? GetStringValue(participant, "ParticipantId")
                          ?? GetStringValue(participant, "Id");

            if (string.IsNullOrWhiteSpace(providerId) || !seenIds.Add(providerId))
            {
                continue; // Skip duplicates and entries without ID
            }

            var providerRecord = new Dictionary<string, object?>
            {
                ["ProviderId"] = providerId,
                ["Name"] = GetStringValue(participant, "Name") ?? GetStringValue(participant, "ParticipantName"),
                ["Email"] = GetStringValue(participant, "Email") ?? GetStringValue(participant, "ParticipantEmail"),
                ["ParticipantType"] = GetStringValue(participant, "ParticipantType") ?? "Provider",
                ["Specialty"] = GetStringValue(participant, "Specialty") ?? GetStringValue(participant, "Role")
            };

            // Add meeting count if available
            if (meetingCountMap.TryGetValue(providerId, out var meetingCount))
            {
                providerRecord["MeetingCount"] = meetingCount;
            }
            else
            {
                providerRecord["MeetingCount"] = 0;
            }

            providers.Add(providerRecord);
        }

        return providers;
    }

    private static Dictionary<string, int> BuildMeetingCountMap(List<Dictionary<string, object?>>? meetingDetails)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (meetingDetails == null)
        {
            return map;
        }

        foreach (var meeting in meetingDetails)
        {
            var providerId = GetStringValue(meeting, "ProviderId")
                          ?? GetStringValue(meeting, "ProviderParticipantId");

            if (string.IsNullOrWhiteSpace(providerId))
            {
                continue;
            }

            map[providerId] = map.GetValueOrDefault(providerId) + 1;
        }

        return map;
    }

    private static bool IsProvider(Dictionary<string, object?> participant)
    {
        var type = GetStringValue(participant, "ParticipantType")
                ?? GetStringValue(participant, "Type")
                ?? GetStringValue(participant, "Role");

        return type?.Equals("Provider", StringComparison.OrdinalIgnoreCase) == true
            || type?.Equals("Doctor", StringComparison.OrdinalIgnoreCase) == true
            || type?.Equals("Clinician", StringComparison.OrdinalIgnoreCase) == true
            || type?.Equals("Therapist", StringComparison.OrdinalIgnoreCase) == true
            || type?.Contains("Provider", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetStringValue(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
}
