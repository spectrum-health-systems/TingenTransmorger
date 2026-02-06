// 260206_code
// 260206_documentation

namespace TingenTransmorger.Database;

/// <summary>
/// Orchestrates construction of the in-memory / on-disk database from cached JSON fragments.
/// </summary>
/// <remarks>
/// This internal static helper coordinates reading cached JSON files produced by the data extraction step, delegates
/// construction of domain-specific collections to dedicated builder classes, aggregates those collections into a
/// database dictionary, and writes the resulting files to the provided master database directory.
/// </remarks>
internal static class DatabaseBuilder
{
    /// <summary>
    /// Builds the application database from cached JSON inputs and writes output files.
    /// </summary>
    /// <param name="tmpDir">
    /// Path to the temporary directory that contains cached JSON input files.
    /// </param>
    /// <param name="masterDbDir">
    /// Path to the master database directory where the final database files will be written.
    /// </param>
    /// <remarks>
    /// <para>
    /// Execution steps:
    /// 1. Read cached JSON lists via <see cref="JsonFileReader.ReadJsonList(string,string)"/>.
    /// 2. Build domain collections using <see cref="PatientsBuilder"/>, <see cref="ProvidersBuilder"/>, and other
    /// specialized builders.
    /// 3. Aggregate sections into a single dictionary representing the database.
    /// 4. Persist the aggregated database using <see cref="JsonFileReader.WriteDatabaseFiles(string,string,System.Collections.Generic.IDictionary{string,object?})"/>.
    /// </para>
    /// <para>
    /// The method relies on the presence and expected structure of the cached JSON files; any IO or parsing errors from
    /// the readers/builders will propagate to the caller.
    /// </para>
    /// </remarks>
    public static void Build(string tmpDir, string masterDbDir)
    {
        // Read cached JSON files
        var participantDetails   = JsonFileReader.ReadJsonList(tmpDir, "Visit_Details-Participant_Details.json");
        var meetingDetails       = JsonFileReader.ReadJsonList(tmpDir, "Visit_Details-Meeting_Details.json");
        var messageDeliveryStats = JsonFileReader.ReadJsonList(tmpDir, "Message_Delivery-Message_Delivery_Stats.json");
        var patients             = PatientsBuilder.Build(tmpDir, participantDetails, messageDeliveryStats);
        var providers            = ProvidersBuilder.Build(tmpDir, participantDetails, meetingDetails);

        var database = new Dictionary<string, object?>
        {
            ["Summary"] = SummaryBuilder.Build(tmpDir),
            ["Patients"] = patients,
            ["Providers"] = providers,
            ["MeetingDetail"] = MeetingDetailBuilder.Build(tmpDir, meetingDetails, patients, providers),
            ["MeetingError"] = MeetingErrorBuilder.Build(tmpDir, patients, providers),
        };

        JsonFileReader.WriteDatabaseFiles(tmpDir, masterDbDir, database);
    }
}