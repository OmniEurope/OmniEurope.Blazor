namespace OmniEurope.Blazor.Components;

/// <summary>
/// A row of buttons, one per character the host supplies (the special letters of the language being
/// typed: the accented letters and quotation marks of French, the umlauts and sharp s of German), each raising <see cref="OnSelect"/> with its character. The package
/// holds no language table: the host chooses the set.
/// </summary>
/// <remarks>
/// A press on a button never takes the focus (the default of <c>mousedown</c> is prevented), so an editor
/// beside the palette keeps its caret and selection and the character lands where the user was typing;
/// <see cref="OmniHtmlEditor.Characters"/> places one under the editing surface. The buttons stay reachable
/// with Tab and act with Enter or Space, like those of the editor's toolbar. When at least one character
/// has a distinct uppercase form, a toggle (<c>aria-pressed</c>) shows and hands out the uppercase forms;
/// a Shift+click does the same for one character.
/// </remarks>
public partial class OmniCharacterPalette
{
    private bool _uppercase;

    /// <summary>
    /// The characters offered, in order: each item is one character or one grapheme (an accented letter,
    /// a ligature, a quotation mark, or a letter followed by a combining mark). Null or empty items
    /// are left out.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Characters { get; set; } = [];

    /// <summary>
    /// Raised with the character chosen, in the case shown: its uppercase form while the uppercase toggle
    /// is pressed or Shift is held, as given otherwise.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSelect { get; set; }

    /// <summary>The accessible name of the palette; null gives the localized "Special characters".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The accessible name of the uppercase toggle; null gives the localized "Uppercase".</summary>
    [Parameter]
    public string? UppercaseLabel { get; set; }

    /// <summary>
    /// Whether the uppercase toggle is offered, true by default. It shows only when at least one character
    /// has a distinct uppercase form; Shift+click keeps working without it.
    /// </summary>
    [Parameter]
    public bool ShowUppercaseToggle { get; set; } = true;

    /// <summary>Whether the uppercase toggle is pressed: the buttons show and hand out the uppercase forms.</summary>
    public bool IsUppercase => _uppercase;

    private string EffectiveLabel => LocalizeOr(Label, "CharacterPaletteLabel");

    private string EffectiveUppercaseLabel => LocalizeOr(UppercaseLabel, "CharacterPaletteUppercase");

    private bool ShowsToggle => ShowUppercaseToggle && Characters.Any(HasUppercase);

    /// <summary>The characters drawn, with their position as the key of their button.</summary>
    private IEnumerable<(string Character, int Index)> Shown =>
        Characters.Select((character, index) => (character, index)).Where(item => !string.IsNullOrEmpty(item.character));

    /// <summary>The uppercase form of a character, by the invariant culture; the character itself when it has none.</summary>
    /// <param name="character">One character or grapheme.</param>
    /// <returns>Its uppercase form.</returns>
    internal static string Uppercase(string character) => character.ToUpper(CultureInfo.InvariantCulture);

    private static bool HasUppercase(string? character) =>
        !string.IsNullOrEmpty(character) && !string.Equals(Uppercase(character), character, StringComparison.Ordinal);

    private string Displayed(string character) => _uppercase ? Uppercase(character) : character;

    private void ToggleCase() => _uppercase = !_uppercase;

    private Task SelectAsync(string character, MouseEventArgs args) =>
        OnSelect.InvokeAsync(_uppercase || args.ShiftKey ? Uppercase(character) : character);
}
