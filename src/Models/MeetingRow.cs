// 260206_code
// 260311_documentation

namespace TingenTransmorger.Models;

/// <summary>Represents a single meeting record sourced from a TeleHealth Visit Details report.</summary>
public class MeetingRow
{
    /// <summary>Gets or sets the unique identifier for the meeting.</summary>
    /// <value>A string containing the meeting ID, or <see cref="string.Empty"/> if not set.</value>
    public string MeetingId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled start time of the meeting.</summary>
    /// <value>A string representation of the scheduled start time, or <see cref="string.Empty"/> if not set.</value>
    public string ScheduledStart { get; set; } = string.Empty;

    /// <summary>Gets or sets the actual start time of the meeting.</summary>
    /// <value>A string representation of the actual start time, or <see cref="string.Empty"/> if not set.</value>
    public string ActualStart { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled end time of the meeting.</summary>
    /// <value>A string representation of the scheduled end time, or <see cref="string.Empty"/> if not set.</value>
    public string ScheduledEnd { get; set; } = string.Empty;

    /// <summary>Gets or sets the actual end time of the meeting.</summary>
    /// <value>A string representation of the actual end time, or <see cref="string.Empty"/> if not set.</value>
    public string ActualEnd { get; set; } = string.Empty;

    /// <summary>Gets or sets the time at which the participant arrived.</summary>
    /// <value>A string representation of the arrival time, or <see cref="string.Empty"/> if not set.</value>
    public string Arrived { get; set; } = string.Empty;

    /// <summary>Gets or sets the time at which the participant dropped from the meeting.</summary>
    /// <value>A string representation of the drop time, or <see cref="string.Empty"/> if not set.</value>
    public string Dropped { get; set; } = string.Empty;

    /// <summary>Gets or sets the duration of the meeting.</summary>
    /// <value>A string representation of the meeting duration, or <see cref="string.Empty"/> if not set.</value>
    public string Duration { get; set; } = string.Empty;

    /// <summary>Gets or sets the status of the meeting.</summary>
    /// <value>A string describing the meeting status, or <see cref="string.Empty"/> if not set.</value>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the meeting has an associated error.</summary>
    /// <value><see langword="true"/> if the meeting has an error; otherwise, <see langword="false"/>.</value>
    public bool HasError { get; set; }

    /// <summary>Gets or sets a value indicating whether the meeting was cancelled.</summary>
    /// <value><see langword="true"/> if the meeting was cancelled; otherwise, <see langword="false"/>.</value>
    public bool IsCancelled { get; set; }

    /// <summary>Gets or sets a value indicating whether the meeting was completed.</summary>
    /// <value><see langword="true"/> if the meeting was completed; otherwise, <see langword="false"/>.</value>
    public bool IsCompleted { get; set; }
}