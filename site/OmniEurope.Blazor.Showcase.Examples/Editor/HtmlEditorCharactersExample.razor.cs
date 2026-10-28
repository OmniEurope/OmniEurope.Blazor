namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The character palette on its own: one that types into a text area and one that only records what it handed out.</summary>
public partial class HtmlEditorCharactersExample
{
    /// <summary>The special letters of German and its quotation marks; the sharp s has no single uppercase letter and stays as it is.</summary>
    private static readonly IReadOnlyList<string> GermanCharacters = ["ä", "ö", "ü", "ß", "„", "“"];

    private string CharacterText { get; set; } = string.Empty;

    /// <summary>What the second palette handed out, in order.</summary>
    private string Chosen { get; set; } = string.Empty;

    private void AppendCharacter(string character) => CharacterText += character;

    private void Choose(string character) => Chosen += character;
}
