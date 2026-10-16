using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Writes a mail template: an optional subject, a body edited with <see cref="OmniHtmlEditor"/>, one button
/// per <see cref="OmniMailVariable"/> that writes its <c>{{token}}</c> at the caret of the subject or the
/// body (whichever was last focused), and a preview tab that shows the mail as
/// <see cref="OmniMailRenderer"/> renders it with each variable's <see cref="OmniMailVariable.SampleValue"/>.
/// </summary>
/// <remarks>
/// The template is what the host stores; sending a mail is rendering it on the server with the real values
/// (<see cref="OmniMailRenderer.Render"/>). The preview draws that rendering in a shadow root and gives its
/// styles through the CSSOM, so it shows the mail's own colours under the page's strict CSP. A variable is
/// inserted in the body only in its visual face; in the source face, its token is typed as text.
/// </remarks>
public partial class OmniMailTemplateEditor
{
    private const string TemplateTab = "template";
    private const string PreviewTab = "preview";
    private readonly string _generatedId = $"omni-mail-template-editor-{Guid.NewGuid():N}";
    private string? _tab = TemplateTab;
    private bool _subjectTarget;
    private OmniHtmlEditor? _editor;
    private ElementReference _preview;
    private IJSObjectReference? _module;
    private string? _drawn;
    private OmniMailRenderResult? _rendered;
    private (string? Value, string? Subject, IReadOnlyList<OmniMailVariable>? Variables, OmniMailTheme? Theme) _renderedFrom;
    private OmniHtmlEditorMode _mode;
    private ElementReference? _tokenSurface;
    private bool _sending;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The body template, sanitised HTML with <c>{{variables}}</c>; bind it with <c>@bind-Value</c>.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Raised with the body template each time it changes.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The subject template, plain text with <c>{{variables}}</c>; bind it with <c>@bind-Subject</c>.</summary>
    [Parameter]
    public string? Subject { get; set; }

    /// <summary>Raised with the subject template each time it changes.</summary>
    [Parameter]
    public EventCallback<string?> SubjectChanged { get; set; }

    /// <summary>Shows the subject field, and the rendered subject above the preview; true by default.</summary>
    [Parameter]
    public bool ShowSubject { get; set; } = true;

    /// <summary>
    /// The variables offered, one button each, in this order. A name must start with a letter and hold only
    /// letters, digits, <c>_</c>, <c>.</c> and <c>-</c>; another throws <see cref="ArgumentException"/>.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniMailVariable>? Variables { get; set; }

    /// <summary>
    /// Shows the « Insert a block » group: a banner in each tone (<c>{{#banner tone}}</c>) and, for each variable of kind
    /// <see cref="OmniMailVariableKind.Link"/>, an action button pointing to it (<c>{{#button name}}</c>), each written in a
    /// paragraph of its own, whose text becomes the banner's or the button's; true by default.
    /// </summary>
    [Parameter]
    public bool ShowBlocks { get; set; } = true;

    /// <summary>The colours of the preview, <see cref="OmniMailTheme.Default"/> when null; give the one the mail will be sent with.</summary>
    [Parameter]
    public OmniMailTheme? Theme { get; set; }

    /// <summary>The name of the editor's tabs; the localized "Mail template" when null.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The commands of the body's toolbar, as <see cref="OmniHtmlEditor.Commands"/>; the editor's default when null.</summary>
    [Parameter]
    public IReadOnlyList<OmniHtmlEditorCommand>? Commands { get; set; }

    /// <summary>The height of the body, in text rows, as <see cref="OmniHtmlEditor.Rows"/>; 12 by default.</summary>
    [Parameter]
    public int Rows { get; set; } = 12;

    /// <summary>Disables the subject, the body and the variable buttons; the preview stays readable.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Lets the template be read and previewed, not changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Shows a « Send a test » button that renders the mail with each variable's <see cref="OmniMailVariable.SampleValue"/>
    /// and hands the <see cref="OmniMailRenderResult"/> to the host, which sends it to whoever checks it in a real mail
    /// client: the package sends nothing. The button stays busy until the returned task completes; without a
    /// handler there is no button.
    /// </summary>
    [Parameter]
    public EventCallback<OmniMailRenderResult> OnSendTest { get; set; }

    /// <summary>
    /// Raised with the exception when <see cref="OnSendTest"/> fails, for the host to say so. Without a handler the
    /// exception propagates to the renderer.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnSendTestError { get; set; }

    /// <summary>The tones of a banner, the first one written by the main part of its button.</summary>
    private static readonly string[] BannerTones = ["info", "success", "warning", "danger"];

    private IEnumerable<OmniMailVariable> LinkVariables => (Variables ?? []).Where(variable => variable.Kind == OmniMailVariableKind.Link);

    private string BannerLabel(string tone) => Localize(tone switch
    {
        "success" => "MailTemplateEditorBannerSuccess",
        "warning" => "MailTemplateEditorBannerWarning",
        "danger" => "MailTemplateEditorBannerDanger",
        _ => "MailTemplateEditorBannerInfo",
    });

    private static string ButtonMarker(OmniMailVariable link) => "{{#button " + link.Name + "}}";

    private string SubjectId => $"{Id ?? _generatedId}-subject";

    private string EffectiveLabel => LocalizeOr(Label, "MailTemplateEditorLabel");

    /// <summary>The mail as the preview shows it, rendered again only when the template, subject, variables or theme change.</summary>
    private OmniMailRenderResult Rendered
    {
        get
        {
            var source = (Value, Subject, Variables, Theme);
            if (_rendered is null || source != _renderedFrom)
            {
                // The renderer of the mail, its looks written in the attribute the preview script reads (strict CSP).
                _rendered = RenderSample(Internal.MailDocument.PreviewLook);
                _renderedFrom = source;
            }

            return _rendered;
        }
    }

    /// <summary>The mail with each variable's sample value (its label in brackets without one), its looks in <paramref name="lookAttribute"/>.</summary>
    private OmniMailRenderResult RenderSample(string lookAttribute)
    {
        var variables = Variables ?? [];
        string Sample(OmniMailVariable variable) => variable.SampleValue ?? $"[{variable.Label}]";
        return Internal.MailDocument.Render(
            Value,
            variables.Where(variable => variable.Kind != OmniMailVariableKind.Block)
                .ToDictionary(variable => variable.Name, variable => (string?)Sample(variable), StringComparer.Ordinal),
            new OmniMailRenderOptions
            {
                Theme = Theme,
                Subject = ShowSubject ? Subject : null,
                Language = CultureInfo.CurrentUICulture.Name,
                Blocks = variables.Where(variable => variable.Kind == OmniMailVariableKind.Block).ToDictionary(
                    variable => variable.Name,
                    variable => (string?)(variable.SampleValue ?? "<p>" + System.Net.WebUtility.HtmlEncode(Sample(variable)) + "</p>"),
                    StringComparer.Ordinal),
            },
            lookAttribute);
    }

    /// <summary>Refuses a variable whose name a template could not write.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        foreach (var variable in Variables ?? [])
        {
            if (!ValidName().IsMatch(variable.Name))
            {
                throw new ArgumentException(
                    $"The mail variable name '{variable.Name}' must start with a letter and hold only letters, digits, '_', '.' and '-'.",
                    nameof(Variables));
            }
        }
    }

    /// <summary>
    /// Marks the tokens of the body's visual face, attached again to each new surface (after the source face), and
    /// draws the rendered mail in the preview while its tab is shown, again only when it changed.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await FollowSurfaceAsync();
            await DrawPreviewAsync();
        }
        catch (JSDisconnectedException)
        {
            _drawn = null;
        }
    }

    private async Task FollowSurfaceAsync()
    {
        ElementReference? surface = _editor is not null && _mode == OmniHtmlEditorMode.Visual ? _editor.SurfaceElement : null;
        if (surface?.Id == _tokenSurface?.Id)
        {
            return;
        }

        // Marked before the call, as the preview: a render that runs meanwhile does not attach it twice.
        _tokenSurface = surface;
        if (surface is { } attached)
        {
            await (await ModuleAsync()).InvokeAsync<int>("attachTokens", attached);
        }
    }

    private async Task DrawPreviewAsync()
    {
        if (_tab != PreviewTab)
        {
            _drawn = null;
            return;
        }

        var html = Rendered.Html;
        if (html == _drawn)
        {
            return;
        }

        // Marked before the call: a render that runs while this one awaits the script does not draw it twice.
        _drawn = html;
        await (await ModuleAsync()).InvokeVoidAsync("render", _preview, html);
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.MailPreview);

    // The editor's face, followed so that the surface of its visual face is marked again when it comes back.
    private void HandleModeChanged(OmniHtmlEditorMode mode) => _mode = mode;

    /// <summary>
    /// Renders the mail as it will be sent (style attributes, sample values) and hands it to the host. A second
    /// press while the host works is ignored by the busy button; a failure goes to <see cref="OnSendTestError"/>.
    /// </summary>
    private async Task SendTestAsync()
    {
        _sending = true;
        try
        {
            await OnSendTest.InvokeAsync(RenderSample(Internal.MailDocument.MailLook));
        }
        catch (Exception exception) when (OnSendTestError.HasDelegate && exception is not OperationCanceledException)
        {
            await OnSendTestError.InvokeAsync(exception);
        }
        finally
        {
            _sending = false;
        }
    }

    private string VariableTitle(OmniMailVariable variable) =>
        string.IsNullOrWhiteSpace(variable.Description) ? variable.Token : Localize("MailTemplateEditorVariableHint", variable.Token, variable.Description);

    private Task HandleValueChangedAsync(string? value)
    {
        Value = value;
        return ValueChanged.InvokeAsync(value);
    }

    private Task HandleSubjectChangedAsync(string? subject)
    {
        Subject = subject;
        return SubjectChanged.InvokeAsync(subject);
    }

    /// <summary>
    /// Writes the variable's token where the caret last was: in the subject when it was the last field
    /// focused, else in the body, at the caret the editor remembers (its end when it never had one), the preview tab included: every panel stays rendered, so the editor keeps its caret.
    /// </summary>
    private async Task InsertAsync(OmniMailVariable variable)
    {
        // The buttons are disabled with the editor (Disabled, ReadOnly): no press reaches here then.
        if (variable.Kind == OmniMailVariableKind.Block)
        {
            await InsertParagraphAsync(variable.Token);
            return;
        }

        if (_subjectTarget && ShowSubject)
        {
            await (await ModuleAsync()).InvokeAsync<bool>("insertAtCaret", SubjectId, variable.Token);
            return;
        }

        if (_editor is not null)
        {
            await _editor.InsertTextAsync(variable.Token);
        }
    }

    /// <summary>Writes a banner's marker in a paragraph of its own, where the caret of the body last was.</summary>
    private Task InsertBannerAsync(string tone) => InsertParagraphAsync("{{#banner " + tone + "}} ");

    /// <summary>Writes an action button's marker, pointing to the link variable, in a paragraph of its own.</summary>
    private Task InsertButtonAsync(OmniMailVariable link) => InsertParagraphAsync(ButtonMarker(link) + " ");

    /// <summary>A block always goes in the body (never in the subject), in a paragraph of its own: its text is escaped, a marker being plain text.</summary>
    private async Task InsertParagraphAsync(string text)
    {
        if (_editor is not null)
        {
            await _editor.InsertHtmlAsync("<p>" + System.Net.WebUtility.HtmlEncode(text) + "</p>");
        }
    }

    /// <summary>Stops marking the body's tokens and releases the script module.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                if (_tokenSurface is { } surface)
                {
                    await _module.InvokeVoidAsync("detachTokens", surface);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_.\-]*$")]
    private static partial Regex ValidName();
}
