namespace OmniEurope.Blazor.Components;

/// <summary>
/// A form field laid out: its label (with the required mark), an optional description, the control and
/// an error line. The control keeps its own id, which <see cref="For"/> names.
/// </summary>
public partial class OmniFormField
{
    /// <summary>
    /// The id of the control the label names (its <c>for</c>). Given, the label itself has the id
    /// <c>{For}-label</c>: a package control that a <c>label for</c> cannot name (the visual face of
    /// <see cref="OmniHtmlEditor"/>, <see cref="OmniRating"/>, <see cref="OmniSelectBar{TValue}"/>, a
    /// selectable card group, a choice list without its own label) and whose id is <see cref="For"/>
    /// points <c>aria-labelledby</c> at it when it has no <c>Label</c> of its own.
    /// </summary>
    [Parameter]
    public string For { get; set; } = string.Empty;

    /// <summary>The label as plain text. Ignored when <see cref="LabelContent"/> is given.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The label as markup, when plain text is not enough; it wins over <see cref="Label"/>.</summary>
    [Parameter]
    public RenderFragment? LabelContent { get; set; }

    /// <summary>The control.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content after the control on the same line: a unit, a button.</summary>
    [Parameter]
    public RenderFragment? EndContent { get; set; }

    /// <summary>
    /// A short explanation between the label and the control, in muted text. Given an <see cref="OmniComponentBase.Id"/>,
    /// it has the id <c>{Id}-description</c>, which the control can name in <c>aria-describedby</c>.
    /// Null or blank, the default, renders nothing.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// A help text for the field, behind a "?" at the end of the label: it shows on hover and on keyboard
    /// focus (the mark is in the Tab order), and is the mark's accessible description. Given an
    /// <see cref="OmniComponentBase.Id"/>, the text has the id <c>{Id}-help-content</c>, which the control
    /// can name in <c>aria-describedby</c>. Null or blank, the default, renders no mark and leaves the
    /// markup unchanged.
    /// </summary>
    [Parameter]
    public string? Help { get; set; }

    /// <summary>
    /// The error shown under the control, which marks the field invalid. It is announced politely when it
    /// appears (a live region, not <c>role="alert"</c>); given an <see cref="OmniComponentBase.Id"/>, the
    /// line has the id <c>{Id}-error</c>, which the control names in <c>aria-describedby</c>. Null or
    /// blank, the default, shows none.
    /// </summary>
    [Parameter]
    public string? Error { get; set; }

    /// <summary>Adds the required mark to the label (the control carries its own requirement).</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Draws the field dimmed (the control is disabled by its own parameter).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? LabelId => string.IsNullOrWhiteSpace(For) ? null : $"{For}-label";
    private Internal.OmniFormFieldLabel? FieldLabel => LabelId is { } labelId ? new(For, labelId) : null;
    private string? DescriptionId =>string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) ? null : $"{Id}-description";
    private string? HelpId => string.IsNullOrWhiteSpace(Id) ? null : $"{Id}-help";
    private string? ErrorId => string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Error) ? null : $"{Id}-error";
}
