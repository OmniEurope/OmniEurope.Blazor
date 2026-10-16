namespace OmniEurope.Blazor.Components;

/// <summary>
/// A value a mail template may insert, written <c>{{Name}}</c> in the template and replaced by
/// <see cref="OmniMailRenderer"/> when the mail is rendered.
/// </summary>
/// <param name="Name">
/// The name written between the double braces: a letter, then letters, digits, <c>_</c>, <c>.</c> or <c>-</c>
/// (<c>client.name</c>, <c>due_date</c>). Compared as written, case included.
/// </param>
/// <param name="Label">What the variable is, as offered to the person who writes the template.</param>
public sealed record OmniMailVariable(string Name, string Label)
{
    /// <summary>
    /// The value shown for the variable in the preview of <see cref="OmniMailTemplateEditor"/>; its label in brackets
    /// when null. For a <see cref="OmniMailVariableKind.Block"/> variable, an HTML fragment, sanitised as the host's
    /// fragments are.
    /// </summary>
    public string? SampleValue { get; init; }

    /// <summary>What the variable holds: a text (the default), an address an action button may point to, or an HTML block.</summary>
    public OmniMailVariableKind Kind { get; init; }

    /// <summary>A line telling where the value comes from, shown under the label in the variable picker.</summary>
    public string? Description { get; init; }

    /// <summary>The text written in a template to insert this variable: <c>{{Name}}</c>.</summary>
    public string Token => "{{" + Name + "}}";
}
