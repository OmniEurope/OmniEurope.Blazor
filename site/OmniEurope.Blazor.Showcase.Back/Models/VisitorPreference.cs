namespace OmniEurope.Blazor.Showcase.Back.Models;

/// <summary>One preference of a visitor, as <c>OmniPreferences</c> synchronizes it.</summary>
public sealed class VisitorPreference
{
    public long Id { get; set; }

    /// <summary>The <see cref="Visitor.Id"/> (hash) of its owner.</summary>
    public required string VisitorId { get; set; }

    public required string Key { get; set; }

    /// <summary>The JSON value; null is a tombstone, kept so the removal reaches the visitor's other tabs.</summary>
    public string? Value { get; set; }

    /// <summary>The browser's date of the change, never replaced by the server clock (except a date in the future).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// When the server recorded the value, on its own clock: the synchronization cursor of the visitor's browsers,
    /// never compared to arbitrate between two values. Assigned under the visitor's write lock, strictly later than
    /// every other <see cref="ReceivedAt"/> of the visitor, so a value that becomes readable later never carries an
    /// earlier one. Rows stored before it existed were given their <see cref="UpdatedAt"/>.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }
}
