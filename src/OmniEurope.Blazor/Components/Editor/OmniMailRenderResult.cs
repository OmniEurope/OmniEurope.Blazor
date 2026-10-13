namespace OmniEurope.Blazor.Components;

/// <summary>A mail rendered by <see cref="OmniMailRenderer.Render"/>, ready to hand to a mail sender.</summary>
/// <param name="Html">The complete HTML document: tables for the layout, every style written on its element.</param>
/// <param name="Text">The plain-text alternative of the body, links followed by their address.</param>
/// <param name="Subject">The rendered subject, on one line; empty when <see cref="OmniMailRenderOptions.Subject"/> is null.</param>
/// <param name="MissingVariables">
/// The names written in the templates for which no value was given (absent or null), each once, in the
/// order they first appear; each was replaced by nothing.
/// </param>
public sealed record OmniMailRenderResult(string Html, string Text, string Subject, IReadOnlyList<string> MissingVariables);
