namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The toolbar the HTML editor example is given: the package's own list, a selection of it, or the same commands reordered.</summary>
public enum HtmlEditorToolbarChoice
{
    /// <summary>Every built-in command, in the package's order.</summary>
    Complete,

    /// <summary>A short selection of the built-in commands.</summary>
    Essential,

    /// <summary>The built-in commands in another order.</summary>
    Reordered
}
