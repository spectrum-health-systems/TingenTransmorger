# Provider email logic

Since there currently isn't an easy way to match providers with email addresses, we won't bother getting them.

This is the original code I was using, which might be helpful in the future?

```csharp
/* DEVNOTE: Why are we collecting email addresses and stats for providers?
    */

// Still collect email data in the background (hidden from UI)
// Display email addresses (hidden from user but still processed)
var emailAddresses = new List<string>();

if (providerDetails.Value.TryGetProperty("EmailAddresses", out var emailAddressesArray))
{
    if (emailAddressesArray.ValueKind == JsonValueKind.Array)
    {
        foreach (var emailEntry in emailAddressesArray.EnumerateArray())
        {
            if (emailEntry.TryGetProperty("Address", out var addressElem))
            {
                var address = addressElem.GetString();
                if (!string.IsNullOrWhiteSpace(address))
                {
                    emailAddresses.Add(address);
                }
            }
        }
    }
}

// Query email failure and delivery stats for all provider email addresses (background processing)
_emailFailures.Clear();
_emailDeliveries.Clear();

foreach (var emailAddress in emailAddresses)
{
    if (emailAddress != "No email addresses on file")
    {
        // DEBUG: Show what we're searching for

        // Query email failures
        var failures = TmDb.GetEmailFailureStats(emailAddress);
        _emailFailures.AddRange(failures);

        // Query email deliveries
        var deliveries = TmDb.GetEmailDeliveryStats(emailAddress);
        _emailDeliveries.AddRange(deliveries);
    }
}
```
