namespace OmniEurope.Blazor.Components;

/// <summary>Which panel an <see cref="OmniHtmlEditor"/> shows under its toolbar besides the link field.</summary>
internal enum HtmlEditorPanel
{
    /// <summary>No panel.</summary>
    None,

    /// <summary>The special characters of <see cref="OmniHtmlEditorAction.InsertSpecialCharacter"/>.</summary>
    Characters,

    /// <summary>The table file of <see cref="OmniHtmlEditorAction.ImportTable"/>.</summary>
    Table
}
