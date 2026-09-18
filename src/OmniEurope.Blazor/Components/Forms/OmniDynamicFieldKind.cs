namespace OmniEurope.Blazor.Components;

/// <summary>The control an <see cref="OmniDynamicField"/> is edited with.</summary>
public enum OmniDynamicFieldKind
{
    /// <summary>A line of text.</summary>
    Text,

    /// <summary>Several lines of text.</summary>
    MultilineText,

    /// <summary>A number, within <see cref="OmniDynamicField.Minimum"/> and <see cref="OmniDynamicField.Maximum"/>.</summary>
    Number,

    /// <summary>Yes or no, as a switch. A required one with no default stays unanswered until touched.</summary>
    Boolean,

    /// <summary>One of <see cref="OmniDynamicField.Options"/>.</summary>
    Choice
}
