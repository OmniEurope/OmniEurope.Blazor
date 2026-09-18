namespace OmniEurope.Blazor.Components;

/// <summary>
/// The layout of a sign-in page: a logo, then a card centred in the page holding the title, the form
/// and a footer. Presentation only; pair it with <see cref="OmniReturnUrl"/> to send the user back to
/// the page they came from once signed in.
/// </summary>
public partial class OmniLoginShell
{
    private readonly string _generatedId = $"omni-login-{Guid.NewGuid():N}";

    /// <summary>The application's logo, above the card.</summary>
    [Parameter]
    public RenderFragment? Logo { get; set; }

    /// <summary>Title of the card; the localized "Sign in" when empty.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>A line under the title.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Heading level of the title; a sign-in page's card title is its first heading.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H1;

    /// <summary>The form: fields, messages and the submit button.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The footer of the card: a forgotten password link, a version line.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    private string TitleId => $"{Id ?? _generatedId}-title";

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Localize("SignIn") : Title;
}
