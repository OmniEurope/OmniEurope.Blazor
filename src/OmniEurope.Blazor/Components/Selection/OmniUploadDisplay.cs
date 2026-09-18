namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniUpload"/> offers the file picker.</summary>
public enum OmniUploadDisplay
{
    /// <summary>A drop zone that also opens the picker on a click.</summary>
    Zone,

    /// <summary>
    /// A read-only field showing the chosen file, welded to a Browse button: a click on either, or
    /// Enter, opens the picker. For a single file only; with <see cref="OmniUpload.Multiple"/> the
    /// zone is kept, since a field shows one name.
    /// </summary>
    Field
}
