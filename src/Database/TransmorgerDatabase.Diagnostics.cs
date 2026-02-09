// 260206_code
// 260206_documentation

using System.Text;
using System.Text.Json;

namespace TingenTransmorger.Database;

/// <summary>Partial class for TransmorgerDatabase diagnostic methods.</summary>
public partial class TransmorgerDatabase
{
    /// <summary>Diagnostic method to check database root properties.</summary>
    public string GetDatabaseStructureDiagnostic()
    {
        if (!_hasData)
            return "No data loaded";
            
        var sb = new StringBuilder();
        sb.AppendLine("Database Root Properties:");
        foreach (var prop in _jsonRoot.EnumerateObject())
        {
            sb.AppendLine($"  - {prop.Name} ({prop.Value.ValueKind})");
            
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine($"      Array Length: {prop.Value.GetArrayLength()}");
                
                // Show first item structure if array is not empty
                if (prop.Value.GetArrayLength() > 0)
                {
                    var firstItem = prop.Value[0];
                    sb.AppendLine($"      First item type: {firstItem.ValueKind}");
                    
                    if (firstItem.ValueKind == JsonValueKind.Object)
                    {
                        sb.AppendLine("      First item properties:");
                        foreach (var itemProp in firstItem.EnumerateObject())
                        {
                            sb.AppendLine($"        - {itemProp.Name}: {itemProp.Value.ValueKind}");
                            
                            // Show string values if short
                            if (itemProp.Value.ValueKind == JsonValueKind.String)
                            {
                                var val = itemProp.Value.GetString();
                                if (!string.IsNullOrEmpty(val) && val.Length < 50)
                                {
                                    sb.AppendLine($"            = \"{val}\"");
                                }
                            }
                        }
                    }
                }
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                sb.AppendLine("      Object properties:");
                foreach (var objProp in prop.Value.EnumerateObject())
                {
                    var count = objProp.Value.ValueKind == JsonValueKind.Array 
                        ? objProp.Value.GetArrayLength() 
                        : 0;
                    sb.AppendLine($"        - {objProp.Name}: {objProp.Value.ValueKind} {(count > 0 ? $"[{count} items]" : "")}");
                }
            }
            
            if (prop.Name == "Summary" && prop.Value.ValueKind == JsonValueKind.Object)
            {
                sb.AppendLine("    Summary Properties (detailed):");
                foreach (var summaryProp in prop.Value.EnumerateObject())
                {
                    var count = summaryProp.Value.ValueKind == JsonValueKind.Array 
                        ? summaryProp.Value.GetArrayLength() 
                        : 0;
                    sb.AppendLine($"      - {summaryProp.Name} ({summaryProp.Value.ValueKind}) {(count > 0 ? $"[{count} items]" : "")}");
                }
            }
        }
        return sb.ToString();
    }
    
    /// <summary>Diagnostic method to show first SMS failure record if any exist.</summary>
    public string GetFirstSmsFailureDiagnostic()
    {
        if (!_hasData)
            return "No data loaded";
            
        var sb = new StringBuilder();
        
        if (!_jsonRoot.TryGetProperty("Summary", out var summary))
        {
            sb.AppendLine("ERROR: No 'Summary' property found in database root");
            return sb.ToString();
        }
        
        if (!summary.TryGetProperty("MessageFailure", out var messageFailure))
        {
            sb.AppendLine("ERROR: No 'MessageFailure' property found in Summary");
            return sb.ToString();
        }
        
        sb.AppendLine($"MessageFailure type: {messageFailure.ValueKind}");
        
        if (messageFailure.ValueKind == JsonValueKind.Array)
        {
            var arrayLength = messageFailure.GetArrayLength();
            sb.AppendLine($"MessageFailure array length: {arrayLength}");
            
            if (arrayLength > 0)
            {
                sb.AppendLine("\n=== First 3 clients ===");
                
                for (int i = 0; i < Math.Min(3, arrayLength); i++)
                {
                    var client = messageFailure[i];
                    sb.AppendLine($"\nClient {i + 1} properties:");
                    foreach (var prop in client.EnumerateObject())
                    {
                        sb.AppendLine($"  - {prop.Name}: {prop.Value.ValueKind}");
                        
                        // Show the actual value for string and number types
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = prop.Value.GetString();
                            if (!string.IsNullOrEmpty(value) && value.Length < 100)
                            {
                                sb.AppendLine($"      Value: \"{value}\"");
                            }
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Number)
                        {
                            sb.AppendLine($"      Value: {prop.Value.GetInt32()}");
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            sb.AppendLine($"      Array Length: {prop.Value.GetArrayLength()}");
                            
                            // Show first item in array if it exists
                            if (prop.Value.GetArrayLength() > 0)
                            {
                                var firstItem = prop.Value[0];
                                sb.AppendLine($"      First item type: {firstItem.ValueKind}");
                                
                                if (firstItem.ValueKind == JsonValueKind.Object)
                                {
                                    sb.AppendLine("      First item properties:");
                                    foreach (var itemProp in firstItem.EnumerateObject())
                                    {
                                        var displayValue = itemProp.Value.ValueKind == JsonValueKind.String
                                            ? $"\"{itemProp.Value.GetString()}\""
                                            : itemProp.Value.ToString();
                                        sb.AppendLine($"        - {itemProp.Name}: {displayValue}");
                                    }
                                }
                            }
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            sb.AppendLine("      Object properties:");
                            foreach (var objProp in prop.Value.EnumerateObject())
                            {
                                sb.AppendLine($"        - {objProp.Name}: {objProp.Value.ValueKind}");
                            }
                        }
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("ERROR: MessageFailure is not an array!");
        }
        
        return sb.ToString();
    }
    
    /// <summary>Searches the database for SMS failure records with phone numbers.</summary>
    public string SearchForSmsFailureRecords()
    {
        if (!_hasData)
            return "No data loaded";
            
        var sb = new StringBuilder();
        sb.AppendLine("=== Searching for SMS Failure Records with Phone Numbers ===\n");
        
        // Check if there's a separate property for detailed SMS failures
        foreach (var rootProp in _jsonRoot.EnumerateObject())
        {
            if (rootProp.Name.Contains("SMS", StringComparison.OrdinalIgnoreCase) ||
                rootProp.Name.Contains("Message", StringComparison.OrdinalIgnoreCase) ||
                rootProp.Name.Contains("Failure", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"Found potentially relevant property: {rootProp.Name} ({rootProp.Value.ValueKind})");
                
                if (rootProp.Value.ValueKind == JsonValueKind.Array && rootProp.Value.GetArrayLength() > 0)
                {
                    var firstItem = rootProp.Value[0];
                    sb.AppendLine($"  First item type: {firstItem.ValueKind}");
                    
                    if (firstItem.ValueKind == JsonValueKind.Object)
                    {
                        sb.AppendLine("  First item properties:");
                        foreach (var itemProp in firstItem.EnumerateObject())
                        {
                            sb.AppendLine($"    - {itemProp.Name}: {itemProp.Value.ValueKind}");
                            
                            if (itemProp.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase) ||
                                itemProp.Name.Contains("Client", StringComparison.OrdinalIgnoreCase) ||
                                itemProp.Name.Contains("Record", StringComparison.OrdinalIgnoreCase))
                            {
                                if (itemProp.Value.ValueKind == JsonValueKind.String)
                                {
                                    sb.AppendLine($"        Value: \"{itemProp.Value.GetString()}\"");
                                }
                                else if (itemProp.Value.ValueKind == JsonValueKind.Array)
                                {
                                    sb.AppendLine($"        Array with {itemProp.Value.GetArrayLength()} items");
                                    if (itemProp.Value.GetArrayLength() > 0)
                                    {
                                        var firstRecord = itemProp.Value[0];
                                        if (firstRecord.ValueKind == JsonValueKind.Object)
                                        {
                                            sb.AppendLine("        First record properties:");
                                            foreach (var recProp in firstRecord.EnumerateObject())
                                            {
                                                var val = recProp.Value.ValueKind == JsonValueKind.String
                                                    ? $"\"{recProp.Value.GetString()}\""
                                                    : recProp.Value.ToString();
                                                sb.AppendLine($"          - {recProp.Name}: {val}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                sb.AppendLine();
            }
        }
        
        // Check Summary for nested properties
        if (_jsonRoot.TryGetProperty("Summary", out var summary))
        {
            sb.AppendLine("Checking Summary for nested SMS/Message properties:");
            foreach (var summaryProp in summary.EnumerateObject())
            {
                if (summaryProp.Name.Contains("SMS", StringComparison.OrdinalIgnoreCase) ||
                    summaryProp.Name.Contains("Failure", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"\n  Found: Summary.{summaryProp.Name} ({summaryProp.Value.ValueKind})");
                    
                    if (summaryProp.Value.ValueKind == JsonValueKind.Object)
                    {
                        sb.AppendLine("    Nested properties:");
                        foreach (var nestedProp in summaryProp.Value.EnumerateObject())
                        {
                            sb.AppendLine($"      - {nestedProp.Name}: {nestedProp.Value.ValueKind}");
                        }
                    }
                }
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>Lists all root-level properties in the database to help identify SMS Stats location.</summary>
    public string ListAllRootProperties()
    {
        if (!_hasData)
            return "No data loaded";
            
        var sb = new StringBuilder();
        sb.AppendLine("=== All Root-Level Database Properties ===\n");
        
        foreach (var prop in _jsonRoot.EnumerateObject())
        {
            sb.AppendLine($"{prop.Name}:");
            sb.AppendLine($"  Type: {prop.Value.ValueKind}");
            
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine($"  Array Length: {prop.Value.GetArrayLength()}");
                
                if (prop.Value.GetArrayLength() > 0)
                {
                    var firstItem = prop.Value[0];
                    sb.AppendLine($"  First Item Type: {firstItem.ValueKind}");
                    
                    if (firstItem.ValueKind == JsonValueKind.Object)
                    {
                        sb.Append("  First Item Properties: ");
                        var propNames = new List<string>();
                        foreach (var itemProp in firstItem.EnumerateObject())
                        {
                            propNames.Add(itemProp.Name);
                        }
                        sb.AppendLine(string.Join(", ", propNames));
                    }
                }
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                sb.Append("  Nested Properties: ");
                var propNames = new List<string>();
                foreach (var nestedProp in prop.Value.EnumerateObject())
                {
                    propNames.Add($"{nestedProp.Name} ({nestedProp.Value.ValueKind})");
                }
                sb.AppendLine(string.Join(", ", propNames));
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}
