using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Editor behind the Date filter type: one text input for a day, a month or a year, read in the date
/// order of the current culture by <see cref="OmniDataGridDateText"/>. The value it reads and writes is
/// the encoded range of <see cref="OmniDataGridDateRange"/>, so the filter travels as the same single
/// string as a date range until the grid resolves it. A text that is not a date marks the input
/// <c>aria-invalid</c> and sends an empty value: nothing is filtered, and the text stays for the
/// reader to correct. A value set from elsewhere (a range picked in the column menu, a cleared filter)
/// replaces the text. <see cref="OmniComponentBase.Id"/> goes on the input; the class and additional
/// attributes too.
/// </summary>
public partial class OmniDataGridFilterDate
{
    private string _text = string.Empty;
    private bool _invalid;
    private string? _synced;

    /// <summary>The range, encoded by <see cref="OmniDataGridDateRange.Join"/>; empty when nothing is filtered.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Raised with the encoded range when the text is read; empty for a blank or invalid text.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>The hint of the input; null gives the format of the current culture (for example <c>dd/mm/yyyy</c>).</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    private string Hint => Placeholder ?? OmniDataGridDateText.Pattern(CultureInfo.CurrentCulture);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.Equals(Value, _synced, StringComparison.Ordinal))
        {
            return;
        }

        _synced = Value;
        // The value is what the text already stands for (the one this input just sent, or an invalid text
        // standing for no filter): the text stays as typed. Otherwise another editor set it.
        if (!string.Equals(ValueOf(_text, out _), Value, StringComparison.Ordinal))
        {
            _text = OmniDataGridDateText.Format(Value, CultureInfo.CurrentCulture);
            _invalid = false;
        }
    }

    private async Task OnChangedAsync(ChangeEventArgs args)
    {
        _text = args.Value?.ToString() ?? string.Empty;
        var value = ValueOf(_text, out _invalid);
        if (!string.Equals(value, Value, StringComparison.Ordinal))
        {
            await ValueChanged.InvokeAsync(value);
        }
    }

    // The encoded range a text stands for; empty for a blank or an invalid text, the latter flagged.
    private static string ValueOf(string text, out bool invalid)
    {
        invalid = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (OmniDataGridDateText.TryParse(text, CultureInfo.CurrentCulture, out var start, out var endExclusive))
        {
            return OmniDataGridDateText.ToValue(start, endExclusive);
        }

        invalid = true;
        return string.Empty;
    }
}
