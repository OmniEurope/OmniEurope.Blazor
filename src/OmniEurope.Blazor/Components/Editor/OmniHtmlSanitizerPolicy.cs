namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniHtmlEditor"/> accepts beyond its own allow-list: extra elements, attributes
/// and classes that a host's markup needs to survive typing, pasting, inserting and commands.
/// </summary>
/// <remarks>
/// The policy only ever widens the built-in allow-list, and never past what the strict CSP and the
/// editor's safety require: a script, a style sheet, an embedded document, a form control or an SVG
/// or MathML island is never allowed, nor is an event handler attribute (<c>on*</c>), <c>style</c>,
/// <c>srcdoc</c>, <c>action</c>, <c>formaction</c> or <c>http-equiv</c>. Naming one of them makes the
/// editor throw <see cref="ArgumentException"/> rather than silently ignore it. Addresses in <c>href</c>,
/// <c>src</c>, <c>cite</c>, <c>poster</c> and <c>longdesc</c> keep the built-in schemes (<c>http</c>,
/// <c>https</c>, <c>mailto</c>, <c>tel</c>, or relative), so a <c>javascript:</c> address is always removed.
/// </remarks>
public sealed record OmniHtmlSanitizerPolicy
{
    /// <summary>Elements kept in addition to the built-in ones, by tag name (<c>aside</c>, <c>img</c>, <c>colgroup</c>).</summary>
    public IReadOnlyList<string> AdditionalTags { get; init; } = [];

    /// <summary>Attributes kept on every allowed element (<c>contenteditable</c>, <c>title</c>).</summary>
    public IReadOnlyList<string> AdditionalAttributes { get; init; } = [];

    /// <summary>
    /// Attributes kept only on the given elements, keyed by tag name: <c>["img"] = ["src", "alt"]</c>
    /// keeps <c>src</c> on an image and still drops it anywhere else.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AdditionalTagAttributes { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>CSS classes kept in addition to the editor's alignment and text size classes.</summary>
    public IReadOnlyList<string> AdditionalCssClasses { get; init; } = [];

    /// <summary>Whether every class is kept, making <see cref="AdditionalCssClasses"/> unnecessary.</summary>
    public bool AllowAnyClass { get; init; }

    /// <summary>Whether every <c>data-*</c> attribute is kept on the allowed elements.</summary>
    public bool AllowDataAttributes { get; init; }
}
