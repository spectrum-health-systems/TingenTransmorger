// 260227_code
// 260311_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.Search partial class contains logic related to searching for patients/providers.
 */
public partial class MainWindow : Window
{
    /// <summary>Refreshes the search results list using the current search type and search box text.</summary>
    /// <remarks>Reads the current search type and search box text before invoking the update pipeline.</remarks>
    private void UpdateSearchResults()
    {
        var searchResults = GetSearchResults(btnSearchToggle.Content.ToString(), txbxSearchBox.Text?.Trim());

        DisplaySearchResults(searchResults);
    }

    /// <summary>Returns a filtered list of search results based on the active search type and text.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Returns an empty list and clears the UI if the search box is blank.</item>
    /// <item>Treats a lone asterisk as a wildcard, returning all results for the active search type.</item>
    /// <item>Routes to patient or provider search methods based on <paramref name="searchType"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="searchType">The active search type; expected to contain 'patient' or 'provider'.</param>
    /// <param name="searchText">The trimmed text from the search box.</param>
    /// <returns>A list of matching patient or provider strings, or an empty list if the search text is blank.</returns>
    private List<string> GetSearchResults(string searchType, string searchText)
    {
        /* Don't try and get search results when there isn't anything to search against.
         */
        if (string.IsNullOrWhiteSpace(txbxSearchBox.Text))
        {
            ClearUi();

            return [];
        }

        /* If the search box contains only an asterisk, treat it as a wildcard to return all results.
         */
        if (txbxSearchBox.Text == "*")
        {
            searchText = string.Empty;
        }

        return searchType.Contains("patient", StringComparison.OrdinalIgnoreCase)
            ? rbtnSearchByName.IsChecked == true
                ? Database.SearchFor.PatientByName(searchText, _tmDb)
                : Database.SearchFor.PatientById(searchText, _tmDb)
            : searchType.Contains("provider", StringComparison.OrdinalIgnoreCase)
                ? rbtnSearchByName.IsChecked == true
                            ? Database.SearchFor.ProviderByName(searchText, _tmDb)
                            : Database.SearchFor.ProviderById(searchText, _tmDb)
                : [];
    }

    /// <summary>Clears and repopulates the search results list box with the given results.</summary>
    /// <remarks>Does not add any items if <paramref name="searchResults"/> is empty.</remarks>
    /// <param name="searchResults">The list of result strings to display.</param>
    private void DisplaySearchResults(List<string> searchResults)
    {
        lstbxSearchResults.Items.Clear();

        if (searchResults.Count != 0)
        {
            foreach (string result in searchResults)
            {
                lstbxSearchResults.Items.Add(result);
            }
        }
    }
}