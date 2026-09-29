namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The error line of a form field, shared by <c>OmniFormField</c> and <c>OmniRadioButtonList</c>: a
/// polite live region always present, which holds, while there is an error, the line with its icon and
/// its text. The region exists before the error does, so the error is announced when it appears, and
/// politely: the accessibility contract reserves assertive announcements (<c>role="alert"</c>) for
/// blocking errors, and a field error is not one. The line carries the id the control names in
/// <c>aria-describedby</c>. Empty, the region takes no room (<c>.omni-form-field__message:empty</c>).
/// </summary>
internal static class OmniFieldError
{
    private const string IconMarkup =
        "<svg class=\"omni-form-field__error-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" focusable=\"false\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"9\" /><path d=\"M12 7.5v5m0 3.5h.01\" /></svg>";

    /// <summary>The live region, with the error line inside when <paramref name="error"/> is not blank.</summary>
    /// <param name="id">The id of the error line; null for none.</param>
    /// <param name="error">The error text; null or blank for no error.</param>
    internal static RenderFragment Region(string? id, string? error) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-form-field__message");
        builder.AddAttribute(2, "aria-live", "polite");
        if (!string.IsNullOrWhiteSpace(error))
        {
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "id", id);
            builder.AddAttribute(5, "class", "omni-form-field__error");
            builder.AddMarkupContent(6, IconMarkup);
            builder.OpenElement(7, "span");
            builder.AddContent(8, error);
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    };
}
