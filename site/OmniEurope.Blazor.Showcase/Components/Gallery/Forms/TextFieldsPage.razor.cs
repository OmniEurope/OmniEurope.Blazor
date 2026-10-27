using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Text fields page: the settings of a text box, an area and a password, then every box type and the states.</summary>
public partial class TextFieldsPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    private OmniTextBoxType BoxType { get; set; } = OmniTextBoxType.Text;

    private bool ShowPlaceholder { get; set; } = true;

    private bool Disabled { get; set; }

    private bool ReadOnly { get; set; }

    private bool Debounced { get; set; }

    /// <summary>Zero follows every keystroke; otherwise the value waits for this calm.</summary>
    private TimeSpan Debounce => Debounced ? TimeSpan.FromMilliseconds(400) : TimeSpan.Zero;

    /// <summary>Zero on the slider means no limit; the example then receives null.</summary>
    private double MaxLengthValue { get; set; } = 40;

    private int? MaxLength => MaxLengthValue > 0 ? (int)MaxLengthValue : null;

    private bool ShowCount { get; set; } = true;

    private double RowsValue { get; set; } = 3;

    private int Rows => (int)RowsValue;

    private bool Revealable { get; set; } = true;

    private bool Required { get; set; } = true;

    private bool ShowDescription { get; set; } = true;

    private bool ShowError { get; set; }

    private IReadOnlyList<OmniOption<OmniTextBoxType>> TypeOptions =>
    [
        new(OmniTextBoxType.Text, Text["TextFieldsTypeText"]),
        new(OmniTextBoxType.Email, Text["TextFieldsTypeEmail"]),
        new(OmniTextBoxType.Tel, Text["TextFieldsTypeTel"]),
        new(OmniTextBoxType.Url, Text["TextFieldsTypeUrl"]),
        new(OmniTextBoxType.Search, Text["TextFieldsTypeSearch"])
    ];

    private string FormatMaxLength(double value) => value > 0
        ? Text["TextFieldsMaxLengthValue", value.ToString("0", CultureInfo.CurrentCulture)]
        : Text["TextFieldsMaxLengthNone"];
}
