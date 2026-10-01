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

    /// <summary>The form: fields, messages, and its submit button in an <see cref="OmniFormActions"/> at the end of the form, aligned to the end.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The footer of the card: a forgotten password link, a version line.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Address of the sign-up page. Set, the footer starts with a discreet text line, "No account? Sign
    /// up", the link on the rank of the forgotten password one; left empty, a site without sign-up shows
    /// nothing. Checked by the same policy as <see cref="OmniLink.Href"/>.
    /// </summary>
    [Parameter]
    public string? SignUpHref { get; set; }

    /// <summary>Text of the sign-up link; the localized "Sign up" when empty.</summary>
    [Parameter]
    public string? SignUpText { get; set; }

    /// <summary>The question before the sign-up link; the localized "No account?" when empty.</summary>
    [Parameter]
    public string? SignUpPrompt { get; set; }

    /// <summary>
    /// What the sign-up line shows in place of the prompt and the link built from
    /// <see cref="SignUpHref"/>, for a sign-up that is not a plain address (an invitation request, a
    /// link that runs an action). The line keeps its place and its discreet style.
    /// </summary>
    [Parameter]
    public RenderFragment? SignUpContent { get; set; }

    private bool HasSignUp => SignUpContent is not null || !string.IsNullOrWhiteSpace(SignUpHref);

    private string TitleId => $"{Id ?? _generatedId}-title";

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Localize("SignIn") : Title;

    private string EffectiveSignUpText => string.IsNullOrWhiteSpace(SignUpText) ? Localize("SignUp") : SignUpText;

    private string EffectiveSignUpPrompt => string.IsNullOrWhiteSpace(SignUpPrompt) ? Localize("SignUpPrompt") : SignUpPrompt;

    // The sign-up line leads the footer, as a text link on the rank of "forgotten password": the web
    // convention, never a second button beside the submit one.
    private RenderFragment? EffectiveFooter => HasSignUp ? BuildFooter : Footer;

    private void BuildFooter(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "class", "omni-login-shell__sign-up");
        if (SignUpContent is not null)
        {
            builder.AddContent(2, SignUpContent);
        }
        else
        {
            builder.OpenElement(3, "span");
            builder.AddContent(4, EffectiveSignUpPrompt);
            builder.CloseElement();
            builder.AddContent(5, " ");
            builder.OpenComponent<OmniLink>(6);
            builder.AddComponentParameter(7, nameof(OmniLink.Href), SignUpHref);
            builder.AddComponentParameter(8, nameof(OmniLink.Class), "omni-login-shell__sign-up-link");
            builder.AddComponentParameter(9, nameof(OmniLink.ChildContent), (RenderFragment)(link => link.AddContent(0, EffectiveSignUpText)));
            builder.CloseComponent();
        }

        builder.CloseElement();
        builder.AddContent(10, Footer);
    }
}
