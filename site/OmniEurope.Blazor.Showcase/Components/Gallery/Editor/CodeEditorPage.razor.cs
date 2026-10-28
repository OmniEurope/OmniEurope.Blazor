using OmniEurope.Blazor.Showcase.Examples.Editor;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The code editor page: the language, read only, line numbers, wrapping, Ctrl+click links and height of an editor, and the text it binds.</summary>
public partial class CodeEditorPage
{
    private static readonly IReadOnlyList<OmniOption<string>> LanguageOptions =
        [.. CodeSamples.Languages.Select(language => new OmniOption<string>(language, language))];

    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private string Language { get; set; } = "yaml";

    private string Code { get; set; } = CodeSamples.Of("yaml");

    private bool ReadOnly { get; set; }

    private bool ShowLineNumbers { get; set; } = true;

    private bool WordWrap { get; set; }

    private bool ShowLinks { get; set; } = true;

    private double HeightValue { get; set; } = 20;

    private int HeightRem => (int)HeightValue;

    /// <summary>Changing the language swaps the sample for the one of that language.</summary>
    private void ChangeLanguage(string language)
    {
        Language = CodeSamples.Languages.Contains(language) ? language : "plaintext";
        Code = CodeSamples.Of(Language);
    }
}
