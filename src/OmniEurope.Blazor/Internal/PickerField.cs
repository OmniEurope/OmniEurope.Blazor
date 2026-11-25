namespace OmniEurope.Blazor.Components;

/// <summary>The text input of a picker field, as the picker that draws it computed it.</summary>
internal readonly record struct PickerFieldInput(
    IReadOnlyDictionary<string, object>? Attributes,
    string? Id,
    string Class,
    string Placeholder,
    string? Value,
    bool Disabled,
    bool ReadOnly,
    string? AriaInvalid,
    string? AriaDescribedBy,
    EventCallback<ChangeEventArgs> OnChange);

/// <summary>The button of a picker field that opens its panel, and the icon it shows.</summary>
internal readonly record struct PickerFieldToggle(
    string Label,
    bool Expanded,
    string PanelId,
    string IconMarkup,
    Action<ElementReference> Capture,
    EventCallback<MouseEventArgs> OnClick,
    EventCallback<KeyboardEventArgs> OnKeyDown);

/// <summary>
/// The field shared by <c>OmniDatePicker</c>, <c>OmniTimePicker</c> and <c>OmniDateTimePicker</c>: the text
/// input and the toggle that opens the panel, inside <c>omni-date__field</c>. Each picker keeps its own root,
/// panel and handlers; this only draws the same elements and attributes the three wrote in their markup, in
/// the picker's own render tree, so the handlers still re-render the picker.
/// </summary>
internal static class PickerField
{
    /// <summary>The calendar icon of the date picker.</summary>
    internal const string CalendarIcon =
        "<svg class=\"omni-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"1.9\" focusable=\"false\" aria-hidden=\"true\"><rect height=\"16\" rx=\"2\" width=\"18\" x=\"3\" y=\"5\"></rect><path d=\"M3 10h18M8 3v4M16 3v4\"></path></svg>";

    /// <summary>The calendar and clock icon of the date and time picker.</summary>
    internal const string CalendarClockIcon =
        "<svg class=\"omni-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"1.9\" focusable=\"false\" aria-hidden=\"true\"><rect height=\"16\" rx=\"2\" width=\"18\" x=\"3\" y=\"5\"></rect><path d=\"M3 10h18M8 3v4M16 3v4\"></path><circle cx=\"16.5\" cy=\"16.5\" r=\"3.5\"></circle><path d=\"M16.5 15v1.5l1 .7\"></path></svg>";

    /// <summary>The clock icon of the time picker.</summary>
    internal const string ClockIcon =
        "<svg class=\"omni-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"1.9\" focusable=\"false\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"9\"></circle><path d=\"M12 7v5l3 2\"></path></svg>";

    /// <summary>Draws the field: the input, then the toggle that is disabled while the field is disabled or read-only.</summary>
    /// <param name="builder">The render tree of the picker.</param>
    /// <param name="input">The input's attributes and change handler.</param>
    /// <param name="toggle">The toggle's state, icon, element capture and handlers.</param>
    internal static void Render(RenderTreeBuilder builder, PickerFieldInput input, PickerFieldToggle toggle)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-date__field");

        builder.OpenElement(2, "input");
        builder.AddMultipleAttributes(3, input.Attributes);
        builder.AddAttribute(4, "id", input.Id);
        builder.AddAttribute(5, "class", input.Class);
        builder.AddAttribute(6, "type", "text");
        builder.AddAttribute(7, "inputmode", "numeric");
        builder.AddAttribute(8, "autocomplete", "off");
        builder.AddAttribute(9, "placeholder", input.Placeholder);
        builder.AddAttribute(10, "value", input.Value);
        builder.AddAttribute(11, "disabled", input.Disabled);
        builder.AddAttribute(12, "readonly", input.ReadOnly);
        builder.AddAttribute(13, "aria-invalid", input.AriaInvalid);
        builder.AddAttribute(14, "aria-describedby", input.AriaDescribedBy);
        builder.AddAttribute(15, "onchange", input.OnChange);
        builder.CloseElement();
        // The line break the markup kept between the input and the toggle, as a text node.
        builder.AddMarkupContent(16, "\n        ");

        builder.OpenElement(17, "button");
        builder.AddAttribute(18, "type", "button");
        builder.AddAttribute(19, "class", "omni-date__toggle");
        builder.AddAttribute(20, "aria-label", toggle.Label);
        builder.AddAttribute(21, "aria-haspopup", "dialog");
        builder.AddAttribute(22, "aria-expanded", toggle.Expanded ? "true" : "false");
        builder.AddAttribute(23, "aria-controls", toggle.Expanded ? toggle.PanelId : null);
        builder.AddAttribute(24, "disabled", input.Disabled || input.ReadOnly);
        builder.AddAttribute(25, "onclick", toggle.OnClick);
        builder.AddAttribute(26, "onkeydown", toggle.OnKeyDown);
        builder.AddElementReferenceCapture(27, toggle.Capture);
        builder.AddMarkupContent(28, toggle.IconMarkup);
        builder.CloseElement();

        builder.CloseElement();
    }
}
