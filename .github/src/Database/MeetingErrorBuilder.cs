namespace TingenTransmorger.Database;

/// <summary>
/// Builds a collection of validation errors and data quality issues discovered during
/// database construction.
/// </summary>
/// <remarks>
/// This builder analyzes patient and provider records to identify missing required fields,
/// invalid data, and other quality issues that should be surfaced to users or administrators.
/// The result is a structured dictionary suitable for inclusion in the transmorger database.
/// </remarks>
internal static class MeetingErrorBuilder
{
    /// <summary>
    /// Analyzes patient and provider records to identify validation errors and data quality issues.
    /// </summary>
    /// <param name="tmpDir">
    /// Temporary directory path (reserved for future use if additional file reads are needed).
    /// </param>
    /// <param name="patients">
    /// List of patient record dictionaries to validate.
    /// </param>
    /// <param name="providers">
    /// List of provider record dictionaries to validate.
    /// </param>
    /// <returns>
    /// A dictionary containing categorized error collections:
    /// - <c>"PatientErrors"</c>: list of validation errors found in patient records
    /// - <c>"ProviderErrors"</c>: list of validation errors found in provider records
    /// - <c>"TotalErrors"</c>: total count of all errors
    /// - <c>"ErrorSummary"</c>: dictionary with counts by error type
    /// </returns>
    /// <remarks>
    /// Common validations performed:
    /// - Missing or invalid email addresses
    /// - Missing required fields (ID, Name)
    /// - Duplicate IDs
    /// - Invalid data formats
    /// </remarks>
    public static Dictionary<string, object?> Build(
        string tmpDir,
        List<Dictionary<string, object?>> patients,
        List<Dictionary<string, object?>> providers)
    {
        var patientErrors = new List<Dictionary<string, object?>>();
        var providerErrors = new List<Dictionary<string, object?>>();
        var errorSummary = new Dictionary<string, int>
        {
            ["MissingEmail"] = 0,
            ["InvalidEmail"] = 0,
            ["MissingId"] = 0,
            ["MissingName"] = 0,
            ["DuplicateId"] = 0
        };

        // Validate patients
        var seenPatientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var patient in patients)
        {
            ValidateRecord(patient, "Patient", "PatientId", seenPatientIds, patientErrors, errorSummary);
        }

        // Validate providers
        var seenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            ValidateRecord(provider, "Provider", "ProviderId", seenProviderIds, providerErrors, errorSummary);
        }

        var totalErrors = patientErrors.Count + providerErrors.Count;

        return new Dictionary<string, object?>
        {
            ["PatientErrors"] = patientErrors,
            ["ProviderErrors"] = providerErrors,
            ["TotalErrors"] = totalErrors,
            ["ErrorSummary"] = errorSummary,
            ["HasErrors"] = totalErrors > 0
        };
    }

    private static void ValidateRecord(
        Dictionary<string, object?> record,
        string recordType,
        string idKey,
        HashSet<string> seenIds,
        List<Dictionary<string, object?>> errorList,
        Dictionary<string, int> errorSummary)
    {
        var id = GetStringValue(record, idKey);
        var name = GetStringValue(record, "Name");
        var email = GetStringValue(record, "Email");

        // Check for missing ID
        if (string.IsNullOrWhiteSpace(id))
        {
            errorList.Add(CreateError(recordType, "MissingId", $"{recordType} record is missing {idKey}", record));
            errorSummary["MissingId"]++;
        }
        else
        {
            // Check for duplicate ID
            if (!seenIds.Add(id))
            {
                errorList.Add(CreateError(recordType, "DuplicateId", $"Duplicate {idKey}: {id}", record));
                errorSummary["DuplicateId"]++;
            }
        }

        // Check for missing name
        if (string.IsNullOrWhiteSpace(name))
        {
            errorList.Add(CreateError(recordType, "MissingName", $"{recordType} {id} is missing Name", record));
            errorSummary["MissingName"]++;
        }

        // Check for missing or invalid email
        if (string.IsNullOrWhiteSpace(email))
        {
            errorList.Add(CreateError(recordType, "MissingEmail", $"{recordType} {id} is missing Email", record));
            errorSummary["MissingEmail"]++;
        }
        else if (!IsValidEmail(email))
        {
            errorList.Add(CreateError(recordType, "InvalidEmail", $"{recordType} {id} has invalid Email: {email}", record));
            errorSummary["InvalidEmail"]++;
        }
    }

    private static Dictionary<string, object?> CreateError(
        string recordType,
        string errorType,
        string message,
        Dictionary<string, object?> record)
    {
        return new Dictionary<string, object?>
        {
            ["RecordType"] = recordType,
            ["ErrorType"] = errorType,
            ["Message"] = message,
            ["RecordId"] = GetStringValue(record, $"{recordType}Id") ?? GetStringValue(record, "Id"),
            ["RecordName"] = GetStringValue(record, "Name"),
            ["Timestamp"] = DateTime.UtcNow.ToString("o")
        };
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Basic email validation regex
            return System.Text.RegularExpressions.Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
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
