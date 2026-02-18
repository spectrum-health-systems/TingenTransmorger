// 260212_code
// 260212_documentation

namespace TingenTransmorger.Database;

/// <summary>Database searches.</summary>
internal static class SearchFor
{
    internal static List<string> PatientByName(string searchText, TransmorgerDatabase tmDb)
    {
        //TODO: Find a way to do this without passing the entire database.
        List<(string name, string id)> allEntries = tmDb.GetPatients();

        return SearchResult(searchText, allEntries, true);
    }

    internal static List<string> PatientById(string searchText, TransmorgerDatabase tmDb)
    {
        List<(string name, string id)> allEntries = tmDb.GetPatients();

        return SearchResult(searchText, allEntries, false);
    }

    /// <summary>Patient/provider search.</summary>
    /// <param name="searchType">The type of search.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="searchByName">Indicates whether to search by name (true) or ID (false).</param>
    internal static List<string> SearchResult(string searchText, List<(string name, string id)> allEntries, bool searchByName)
    {
        var nameAndId = new List<(string name, string id)>();

        nameAndId = searchByName
            ? [.. allEntries.Where(p => p.name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.name)]
            : [.. allEntries.Where(p => p.id.Contains(searchText, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.name)];

        List<string> resultList = [];

        foreach (var (name, id) in nameAndId)
        {
            resultList.Add($"{name} ({id})");
        }

        return resultList;
    }
}