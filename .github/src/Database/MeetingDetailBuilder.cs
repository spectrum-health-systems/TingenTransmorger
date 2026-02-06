// 260206_code
// 260206_documentation

namespace TingenTransmorger.Database;

/// <summary>
/// Helper used by the database layer to construct a consolidated map of meeting-related
/// information from raw input collections.
/// </summary>
/// <remarks>
/// The <see cref="Build"/> method aggregates provided meeting detail dictionaries,
/// patient records and provider records into a single <see cref="Dictionary{String, Object}"/>
/// whose shape is intended for persistence to outputs such as `transmorger.json` and
/// `transmorger.db`.
///
/// Implementations should treat input collections as read-only and must not mutate the
/// supplied lists or dictionaries. The method returns a stable output shape so downstream
/// consumers (writers, serializers) can reliably find the expected entries.
/// </remarks>
internal static class MeetingDetailBuilder
{
    /// <summary>
    /// Build an aggregated dictionary containing meeting-level data, patient entries and
    /// provider entries suitable for serialization or database insertion.
    /// </summary>
    /// <param name="tmpDir">
    /// Path to a temporary directory available for intermediate files or resources used
    /// while building the result. The caller should guarantee this directory exists and
    /// is writable if the implementation needs to create temporary artifacts.
    /// </param>
    /// <param name="meetingDetails">
    /// Optional list of meeting detail dictionaries to include. Each dictionary maps string
    /// keys to values of arbitrary types. If <c>null</c>, no meeting-detail entries are merged.
    /// </param>
    /// <param name="patients">
    /// List of patient dictionaries. Each dictionary represents one patient record with
    /// string keys and values of arbitrary types. These will be included under a patient
    /// collection key in the returned dictionary.
    /// </param>
    /// <param name="providers">
    /// List of provider dictionaries. Each dictionary represents one provider record and
    /// will be included under a provider collection key in the returned dictionary.
    /// </param>
    /// <returns>
    /// A dictionary containing aggregated meeting information. Typical keys consumers
    /// should expect (implementation-dependent but recommended) include:
    /// - <c>"Meetings"</c>: a list of meeting detail records with enriched participant information.
    /// - <c>"TotalMeetings"</c>: total count of meetings.
    /// - <c>"TotalPatients"</c>: total count of unique patients.
    /// - <c>"TotalProviders"</c>: total count of unique providers.
    ///
    /// Values may be <c>null</c> when the underlying information is absent. The returned
    /// dictionary is the canonical object that persistence layers should serialize or store.
    /// </returns>
    /// <remarks>
    /// If downstream persistence (for example writing `transmorger.json` or insertion into
    /// `transmorger.db`) appears to miss data such as Patients, Providers, MeetingDetail or
    /// MeetingError, verify that this method includes those keys with the expected types and
    /// that the writer/serializer examines the full returned dictionary rather than a subset.
    /// </remarks>
    public static Dictionary<string, object?> Build(
        string tmpDir,
        List<Dictionary<string, object?>>? meetingDetails,
        List<Dictionary<string, object?>> patients,
        List<Dictionary<string, object?>> providers)
    {
        var result = new Dictionary<string, object?>();

        // Build lookup maps for enriching meeting data
        var patientMap = BuildParticipantMap(patients, "PatientId");
        var providerMap = BuildParticipantMap(providers, "ProviderId");

        var enrichedMeetings = new List<Dictionary<string, object?>>();

        if (meetingDetails != null && meetingDetails.Count > 0)
        {
            foreach (var meeting in meetingDetails)
            {
                var enrichedMeeting = new Dictionary<string, object?>(meeting);

                // Enrich with patient info
                var patientId = GetStringValue(meeting, "PatientId")
                             ?? GetStringValue(meeting, "PatientParticipantId");
                if (!string.IsNullOrWhiteSpace(patientId) && patientMap.TryGetValue(patientId, out var patient))
                {
                    enrichedMeeting["PatientName"] = GetStringValue(patient, "Name");
                    enrichedMeeting["PatientEmail"] = GetStringValue(patient, "Email");
                }

                // Enrich with provider info
                var providerId = GetStringValue(meeting, "ProviderId")
                              ?? GetStringValue(meeting, "ProviderParticipantId");
                if (!string.IsNullOrWhiteSpace(providerId) && providerMap.TryGetValue(providerId, out var provider))
                {
                    enrichedMeeting["ProviderName"] = GetStringValue(provider, "Name");
                    enrichedMeeting["ProviderEmail"] = GetStringValue(provider, "Email");
                    enrichedMeeting["ProviderSpecialty"] = GetStringValue(provider, "Specialty");
                }

                enrichedMeetings.Add(enrichedMeeting);
            }
        }

        result["Meetings"] = enrichedMeetings;
        result["TotalMeetings"] = enrichedMeetings.Count;
        result["TotalPatients"] = patients.Count;
        result["TotalProviders"] = providers.Count;

        return result;
    }

    private static Dictionary<string, Dictionary<string, object?>> BuildParticipantMap(
        List<Dictionary<string, object?>> participants,
        string idKey)
    {
        var map = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var participant in participants)
        {
            var id = GetStringValue(participant, idKey);
            if (!string.IsNullOrWhiteSpace(id))
            {
                map[id] = participant;
            }
        }

        return map;
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