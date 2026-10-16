namespace OmniEurope.Blazor.Components;

/// <summary>What an <see cref="OmniMailVariable"/> holds, which decides how <see cref="OmniMailTemplateEditor"/> offers it.</summary>
public enum OmniMailVariableKind
{
    /// <summary>A text, written as text wherever its token stands (a value is never read as markup).</summary>
    Text,

    /// <summary>
    /// An address (<c>https://…</c>, <c>mailto:…</c>): a text variable that the editor also offers as the target of an
    /// action button (<c>{{#button name}}</c>).
    /// </summary>
    Link,

    /// <summary>
    /// A pre-rendered HTML fragment the host gives in <see cref="OmniMailRenderOptions.Blocks"/>, sanitised then inserted
    /// as a block in place of the paragraph that holds its token alone; the editor inserts its token in a paragraph of its own.
    /// </summary>
    Block,
}
