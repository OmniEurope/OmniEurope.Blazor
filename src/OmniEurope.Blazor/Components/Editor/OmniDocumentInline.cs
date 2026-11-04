namespace OmniEurope.Blazor.Components;

/// <summary>
/// An element of a paragraph: an <see cref="OmniDocumentRun"/> of text or an <see cref="OmniDocumentField"/>.
/// The editor accepts these two kinds only.
/// </summary>
/// <param name="Id">The identifier of the element, unique among the runs and fields of the whole document; the change set names a change by it.</param>
public abstract record OmniDocumentInline(string Id);
