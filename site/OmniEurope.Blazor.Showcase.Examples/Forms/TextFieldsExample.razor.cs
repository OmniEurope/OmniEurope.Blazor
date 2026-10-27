using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A text box, a text area, a password and a bare label with a box, driven by parameters.</summary>
public partial class TextFieldsExample
{
    private const string ErrorSample = "This value is not accepted.";

    /// <summary>The type of the single-line box; it changes the mobile keyboard and the autofill, never the value.</summary>
    [Parameter]
    public OmniTextBoxType BoxType { get; set; } = OmniTextBoxType.Text;

    /// <summary>Whether the fields show a sample while they are empty.</summary>
    [Parameter]
    public bool ShowPlaceholder { get; set; } = true;

    /// <summary>Whether the fields can be neither focused nor edited.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the fields can be focused and copied but not edited.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>How long the bound value waits after the last keystroke; zero follows every keystroke.</summary>
    [Parameter]
    public TimeSpan Debounce { get; set; }

    /// <summary>The longest text the browser accepts, or <see langword="null"/> for no limit.</summary>
    [Parameter]
    public int? MaxLength { get; set; } = 40;

    /// <summary>Whether the text area shows a character counter.</summary>
    [Parameter]
    public bool ShowCount { get; set; } = true;

    /// <summary>The height of the text area, in lines.</summary>
    [Parameter]
    public int Rows { get; set; } = 3;

    /// <summary>Whether the password has a button that shows what was typed.</summary>
    [Parameter]
    public bool Revealable { get; set; } = true;

    /// <summary>Whether the labels carry the asterisk; it validates nothing by itself.</summary>
    [Parameter]
    public bool Required { get; set; } = true;

    /// <summary>Whether a help line sits under each label, linked to its field for screen readers.</summary>
    [Parameter]
    public bool ShowDescription { get; set; } = true;

    /// <summary>Whether each form field shows an error and takes its invalid look.</summary>
    [Parameter]
    public bool ShowError { get; set; }

    private string TextValue { get; set; } = string.Empty;

    private string AreaValue { get; set; } = string.Empty;

    private string PasswordValue { get; set; } = string.Empty;

    private string BareValue { get; set; } = string.Empty;

    private static string PlaceholderFor(OmniTextBoxType type) => type switch
    {
        OmniTextBoxType.Email => "camille@example.com",
        OmniTextBoxType.Tel => "+32 2 000 00 00",
        OmniTextBoxType.Url => "https://example.com",
        OmniTextBoxType.Search => "Filter the list",
        _ => "Camille Durand"
    };

    /// <summary>The autofill token follows the type, as a real form would set it.</summary>
    private static string? AutocompleteFor(OmniTextBoxType type) => type switch
    {
        OmniTextBoxType.Email => "email",
        OmniTextBoxType.Tel => "tel",
        OmniTextBoxType.Url => "url",
        _ => null
    };

    /// <summary>A form field names its help line "{Id}-description"; the control links to it while it shows.</summary>
    private string? DescribedBy(string fieldId) => ShowDescription ? $"{fieldId}-description" : null;
}
