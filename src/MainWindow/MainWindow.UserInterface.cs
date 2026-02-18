// 260212_code
// 260212_documentation

using System.Windows;

/* I've moved the MainWindow partial classes to MainWindow/ to keep the code organized, but I'm leaving the namespace as
 * TingenTransmorger instead of TingenTransmorger.MainWindow to avoid confusion with the MainWindow class.
 */
namespace TingenTransmorger;

/// <summary>MainWindow user interface logic.</summary>
/// <remarks>
///     This is a partial class that handles the user interface functionality of the MainWindow.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>Setup the initial user interface so the right panel is blank.</summary>
    private void SetupInitialUi()
    {
        rbtnSearchByName.IsChecked           = true;
        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility  = Visibility.Collapsed;
    }

    /// <summary>Clears user interface components.</summary>
    private void ClearUi()
    {
        txbxSearchBox.Text = string.Empty; // This will fire off the TextChanged event

        //lstbxSearchResults.Items.Clear();

        spnlPatientProviderDetailsComponents.Visibility  = Visibility.Collapsed;
        spnlMeetingComponents.Visibility = Visibility.Collapsed;
        spnlMeetingDetailsComponents.Visibility  = Visibility.Collapsed;
    }

    private void SetupPatientDetailUi(string patientName, string patientId)
    {

        lblPatientProviderKey.Content      = "PATIENT";
        lblPatientProviderNameValue.Content   = patientName;
        lblPatientProviderIdValue.Content     = patientId;
        spnlPatientProviderDetailsComponents.Visibility = Visibility.Visible;
        spnlPatientPhoneComponents.Visibility   = Visibility.Visible;
        spnlPatientEmailComponents.Visibility   = Visibility.Visible;
    }


    /// <summary>Display search results..</summary>
    /// <remarks>
    ///     This method is called when the user types in the search text box. It filters and displays results based on
    ///     the current search mode and search type (by name or ID).
    /// </remarks>
    /// <param name="searchText">Contents of the search box.</param>
    private void DisplaySearchResults(string searchType, string searchText)
    {
        List<string> searchResults;

        if (searchType.Contains("patient", StringComparison.OrdinalIgnoreCase))
        {
            searchResults = rbtnSearchByName.IsChecked == true
                ? Database.SearchFor.PatientByName(searchText, TmDb)
                : Database.SearchFor.PatientById(searchText, TmDb);
        }
        else if (searchType.Contains("provider", StringComparison.OrdinalIgnoreCase))
        {
            searchResults =(rbtnSearchByName.IsChecked == true)
                ? Database.SearchFor.ProviderByName(searchText, TmDb)
                : Database.SearchFor.ProviderById(searchText, TmDb);
        }
        else
        {
            // Bad
            searchResults = new List<string>();
        }



        lstbxSearchResults.Items.Clear();



        foreach (string result in searchResults)
        {
            lstbxSearchResults.Items.Add(result);
        }
    }
}