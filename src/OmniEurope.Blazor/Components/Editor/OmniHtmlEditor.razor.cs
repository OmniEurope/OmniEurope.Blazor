using System.Text.Json;

namespace OmniEurope.Blazor.Components;

public partial class OmniHtmlEditor
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private ElementReference _source;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("HtmlEditorLabel")
        : Label;
    [Parameter] public int Rows { get; set; } = 12;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool ShowPreview { get; set; } = true;
    [Parameter] public bool EnableBold { get; set; } = true;
    [Parameter] public bool EnableItalic { get; set; } = true;
    [Parameter] public bool EnableSubscript { get; set; } = true;
    [Parameter] public bool EnableSuperscript { get; set; } = true;
    [Parameter] public bool EnableIndent { get; set; } = true;
    [Parameter] public bool EnableOutdent { get; set; } = true;
    [Parameter] public string? AriaDescribedBy { get; set; }
    [Parameter] public IReadOnlyList<OmniHtmlEditorTool> CustomTools { get; set; } = Array.Empty<OmniHtmlEditorTool>();

    private Task HandleInputAsync(ChangeEventArgs args) => Disabled
        ? Task.CompletedTask
        : CommitAsync(OmniHtmlSanitizer.Sanitize(args.Value?.ToString()));
    private async Task WrapSelectionAsync(string tag)
    {
        if (Disabled)
        {
            return;
        }

        await using var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omniInterop.js");
        var result = await module.InvokeAsync<JsonElement>("wrapTextSelection", _source, $"<{tag}>", $"</{tag}>");
        var value = result.GetProperty("value").GetString() ?? string.Empty;
        var start = result.GetProperty("selectionStart").GetInt32();
        var end = result.GetProperty("selectionEnd").GetInt32();
        await CommitAsync(OmniHtmlSanitizer.Sanitize(value));
        await InvokeAsync(StateHasChanged);
        await module.InvokeVoidAsync("restoreTextSelection", _source, start, end);
    }

    private Task BoldAsync() => WrapSelectionAsync("strong");
    private Task ItalicAsync() => WrapSelectionAsync("em");
    private Task SubscriptAsync() => WrapSelectionAsync("sub");
    private Task SuperscriptAsync() => WrapSelectionAsync("sup");
    private Task IndentAsync() => WrapSelectionAsync("blockquote");
    private Task OutdentAsync() => ApplyAsync(value => value.StartsWith("<blockquote>", StringComparison.OrdinalIgnoreCase) && value.EndsWith("</blockquote>", StringComparison.OrdinalIgnoreCase) ? value[12..^13] : value);

    private Task ApplyAsync(Func<string, string> transform) => Disabled
        ? Task.CompletedTask
        : CommitAsync(OmniHtmlSanitizer.Sanitize(transform(CurrentValue ?? string.Empty)));

    private Task CommitAsync(string value)
    {
        if (!string.Equals(CurrentValue, value, StringComparison.Ordinal))
        {
            _undo.Push(CurrentValue ?? string.Empty);
            _redo.Clear();
            CurrentValue = value;
        }
        return Task.CompletedTask;
    }

    private Task UndoAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        if (_undo.TryPop(out var previous))
        {
            _redo.Push(CurrentValue ?? string.Empty);
            CurrentValue = previous;
        }
        return Task.CompletedTask;
    }

    private Task RedoAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        if (_redo.TryPop(out var next))
        {
            _undo.Push(CurrentValue ?? string.Empty);
            CurrentValue = next;
        }
        return Task.CompletedTask;
    }

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = OmniHtmlSanitizer.Sanitize(value);
        validationErrorMessage = null!;
        return true;
    }
}
