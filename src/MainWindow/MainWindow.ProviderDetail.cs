// 260225_code
// 260225_documentation

using System.Text.Json;
using System.Windows;

namespace TingenTransmorger;

/* The MainWindow.ProviderDetails partial class contains logic related to displaying provider details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary> Currently selected provider name.</summary>
    private string _currentProviderName = string.Empty;

    /// <summary>Currently selected provider ID.</summary>
    private string _currentProviderId = string.Empty;

    /// <summary>Displays provider details in the UI.</summary>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        _currentProviderName = providerName;
        _currentProviderId   = providerId;

        // Get provider details from database
        JsonElement? providerDetails = TmDb.GetProviderDetails(providerName);

        if (providerDetails == null)
        {
            StopApp($"Critical error! [MW8001]");
        }

        SetProviderDetailUi(providerName, providerId);
        /* There isn't a way to easily match providers to their email addresses, so we aren't going to do that for now.
         * Eventually we should, and this is (probably) where that logic should go. For now I've put the code I was
         * working on in .github/Development/ProviderEmailLogic.md.
         */
        DisplayProviderMeetingResults();
    }
}
