// 260206_code
// 260206_documentation

namespace TingenTransmorger.Models;

/// <summary>Represents a row in the patient meetings table.</summary>
public class PatientMeetingRow
{
    /// <summary>Gets or sets the meeting ID.</summary>
    public string MeetingId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled start time.</summary>
    public string Start { get; set; } = string.Empty;

    /// <summary>Gets or sets the time the patient arrived.</summary>
    public string Arrived { get; set; } = string.Empty;

    /// <summary>Gets or sets the time the patient dropped from the meeting.</summary>
    public string Dropped { get; set; } = string.Empty;

    /// <summary>Gets or sets the duration of the meeting.</summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>Gets or sets the meeting status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this meeting has an error in the MeetingError collection.</summary>
    public bool HasError { get; set; }

    /// <summary>Gets or sets whether this meeting has been cancelled.</summary>
    public bool IsCancelled { get; set; }

    /// <summary>Gets or sets whether this meeting has been completed.</summary>
    public bool IsCompleted { get; set; }
}


