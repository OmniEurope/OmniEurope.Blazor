namespace OmniEurope.Blazor.Components;

/// <summary>
/// How wide an <see cref="OmniDialog"/> may grow. Every width is a ceiling, capped by the viewport;
/// below a 40rem viewport a dialog takes the full width whatever its size.
/// </summary>
public enum OmniDialogSize
{
    /// <summary>Up to 40rem, the width a dialog always had. The default.</summary>
    Medium = 0,

    /// <summary>Up to 25rem, for a short question or a single field.</summary>
    Small,

    /// <summary>Up to 56rem, for a form in two columns or a short table.</summary>
    Large,

    /// <summary>Up to 72rem, for a wide table or a side-by-side comparison.</summary>
    ExtraLarge,

    /// <summary>Up to 96% of the viewport width (100rem at most), and 1rem short of its full height.</summary>
    FullWidth
}
