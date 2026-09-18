namespace OmniEurope.Blazor.Components;

/// <summary>One point or segment of an <see cref="OmniStatusStrip"/>.</summary>
public sealed record OmniStatusStripItem
{
    /// <summary>The host's status key, looked up in <see cref="OmniStatusStrip.Tones"/> and <see cref="OmniStatusStrip.Pulsing"/>.</summary>
    public required string Status { get; init; }

    /// <summary>What the item stands for, read as its accessible name and shown as its tooltip.</summary>
    public required string Label { get; init; }

    /// <summary>Where the item leads; null leaves it a plain mark, or a button when the strip has <see cref="OmniStatusStrip.OnItemClick"/>.</summary>
    public string? Href { get; init; }
}
