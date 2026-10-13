namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniMailRenderer.Render"/> lays out a mail.</summary>
public sealed record OmniMailRenderOptions
{
    /// <summary>The colours, font and radius of the mail; <see cref="OmniMailTheme.Default"/> when null.</summary>
    public OmniMailTheme? Theme { get; init; }

    /// <summary>
    /// The subject template, plain text with <c>{{variables}}</c>: rendered into
    /// <see cref="OmniMailRenderResult.Subject"/> and into the document's <c>title</c>. Null renders no subject.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// The line a mail client shows after the subject in its list of mails, plain text with
    /// <c>{{variables}}</c>, hidden in the mail itself. Null writes none.
    /// </summary>
    public string? Preheader { get; init; }

    /// <summary>The document's language (<c>lang</c>); the current UI culture's name when null.</summary>
    public string? Language { get; init; }

    /// <summary>The widest the mail's card grows, in pixels, 600 by default (the width mail clients are designed for); 320 to 1200.</summary>
    public int Width { get; init; } = 600;
}
