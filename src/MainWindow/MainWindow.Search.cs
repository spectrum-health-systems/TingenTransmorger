// 260227_code
// 260227_documentation

using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.Search partial class contains logic related to searching for patients/providers.
 */
public partial class MainWindow : Window
{
    /// <summary>Modifies the search results based on the current search type and search text.</summary>
    private void UpdateSearchResults()
    {
        var searchResults = GetSearchResults(btnSearchToggle.Content.ToString(), txbxSearchBox.Text?.Trim());

        DisplaySearchResults(searchResults);
    }

    /// <summary>Get a list of patient/provider search results.</summary>
    /// <param name="searchType">The type of search.</param>
    /// <param name="searchText">Contents of the search box.</param>
    /// <returns>The search results.</returns>
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

    /// <summary>Display search results.</summary>
    /// <param name="searchResults">The list of search results to display.</param>
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