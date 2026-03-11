// 260212_code
// 260212_documentation

namespace TingenTransmorger.Database;

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
        //TODO: Find a way to do this without passing the entire database.
        List<(string name, string id)> allEntries = tmDb.GetPatients();

        return SearchResult(searchText, allEntries, false);
    }

    internal static List<string> ProviderByName(string searchText, TransmorgerDatabase tmDb)
    {
        //TODO: Find a way to do this without passing the entire database.
        List<(string name, string id)> allEntries = tmDb.GetProviders();

        return SearchResult(searchText, allEntries, true);
    }

    internal static List<string> ProviderById(string searchText, TransmorgerDatabase tmDb)
    {
        //TODO: Find a way to do this without passing the entire database.
        List<(string name, string id)> allEntries = tmDb.GetProviders();

        return SearchResult(searchText, allEntries, false);
    }

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