// 260212_code
// 260212_documentation

namespace TingenTransmorger.Database;

/// <summary>Database searches.</summary>
internal static class SearchFor
{
    /// <summary>Patient search.</summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="tmDb">The Transmorger database instance.</param>
    /// <param name="searchByName">Indicates whether to search by patient name (true) or patient ID (false).</param>
    internal static List<string> Patients(string searchText, TransmorgerDatabase tmDb, bool searchByName)
    {
        //TODO: Find a way to do this without passing the entire database.
        List<(string PatientName, string PatientId)> allPatients = tmDb.GetPatients();

        var patientNameAndId = new List<(string PatientName, string PatientId)>();

        patientNameAndId = searchByName
            ? [.. allPatients.Where(p => p.PatientName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.PatientName)]
            : [.. allPatients.Where(p => p.PatientId.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.PatientName)];

        List<string> patientList = [];

        foreach (var (PatientName, PatientId) in patientNameAndId)
        {
            patientList.Add($"{PatientName} ({PatientId})");
        }

        return patientList;
    }

    /// <summary>Provider search.</summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="tmDb">The Transmorger database instance.</param>
    /// <param name="searchByName">Indicates whether to search by provider name (true) or provider ID (false).</param>
    internal static List<string> Providers(string searchText, TransmorgerDatabase tmDb, bool searchByName)
    {
        //TODO: Find a way to do this without passing the entire database.
        List<(string ProviderName, string ProviderId)> allProviders = tmDb.GetProviders();

        var providerNameAndId = new List<(string ProviderName, string ProviderId)>();

        providerNameAndId = searchByName
            ? [.. allProviders.Where(p => p.ProviderName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.ProviderName)]
            : [.. allProviders.Where(p => p.ProviderId.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.ProviderName)];

        List<string> patientList = [];

        foreach (var (ProviderName, ProviderId) in providerNameAndId)
        {
            patientList.Add($"{ProviderName} ({ProviderId})");
        }

        return patientList;
    }


}
