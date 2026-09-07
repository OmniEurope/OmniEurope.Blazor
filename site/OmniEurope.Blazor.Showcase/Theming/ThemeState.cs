using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// Holds the theme the visitor is building and pushes it onto the live page.
/// </summary>
/// <remarks>
/// Values are written through the CSSOM rather than through a style attribute or an injected style
/// element: the site ships a strict policy that forbids both, and the CSSOM path is the one the
/// policy leaves open.
/// </remarks>
public sealed class ThemeState(ThemeTokenReader reader, IJSRuntime js)
{
    private const string StorageKey = "omnieurope.showcase.theme";

    private readonly Dictionary<string, string> _overrides = new(StringComparer.Ordinal);
    private IReadOnlyList<ThemeToken> _tokens = [];

    /// <summary>Raised whenever the theme changes, so open editors redraw.</summary>
    public event Action? Changed;

    /// <summary>The token catalogue read from the stylesheet.</summary>
    public IReadOnlyList<ThemeToken> Tokens => _tokens;

    /// <summary>The mode the visitor is previewing.</summary>
    public ThemeMode Mode { get; private set; } = ThemeMode.Light;

    /// <summary>The palette currently applied, or null once the visitor edits freely.</summary>
    public ThemePreset? Preset { get; private set; }

    /// <summary>The tokens the visitor moved away from their shipped value.</summary>
    public IReadOnlyDictionary<string, string> Overrides => _overrides;

    /// <summary>
    /// Loads the catalogue and replays whatever the visitor had left behind in this browser.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _tokens = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var stored = await js.InvokeAsync<string?>("omniShowcaseTheme.load", cancellationToken, StorageKey)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            Restore(stored);
        }

        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The value in force for a token, shipped value included.</summary>
    public string ValueOf(ThemeToken token) =>
        _overrides.TryGetValue(token.Name, out var value) ? value : token.DefaultValue;

    /// <summary>Moves one token and repaints the page.</summary>
    public async Task SetAsync(ThemeToken token, string value, CancellationToken cancellationToken = default)
    {
        if (string.Equals(value, token.DefaultValue, StringComparison.Ordinal))
        {
            _overrides.Remove(token.Name);
        }
        else
        {
            _overrides[token.Name] = value;
        }

        Preset = null;
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Drops every override and returns to the shipped theme.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _overrides.Clear();
        Preset = null;
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Applies a ready-made palette in the mode being previewed.</summary>
    public async Task ApplyAsync(ThemePreset preset, CancellationToken cancellationToken = default)
    {
        _overrides.Clear();
        foreach (var (name, value) in preset.For(Mode))
        {
            _overrides[name] = value;
        }

        Preset = preset;
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Switches mode, carrying the applied palette across to its other half.</summary>
    public async Task SetModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        Mode = mode;
        if (Preset is { } preset)
        {
            _overrides.Clear();
            foreach (var (name, value) in preset.For(mode))
            {
                _overrides[name] = value;
            }
        }

        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The stylesheet the visitor can paste into their own application. Only the tokens actually
    /// moved are written, so the export stays a patch over the shipped theme rather than a copy of
    /// it that would freeze every future change.
    /// </summary>
    public string ExportCss()
    {
        if (_overrides.Count == 0)
        {
            return "/* No token changed: the shipped theme is already in force. */";
        }

        var builder = new StringBuilder();
        builder.AppendLine("/* OmniEurope.Blazor theme overrides. Load after omnieurope.blazor.css. */");
        builder.AppendLine(Mode is ThemeMode.Dark ? "[data-omni-theme=\"dark\"] {" : ":root {");
        foreach (var token in _tokens.Where(token => _overrides.ContainsKey(token.Name)))
        {
            builder.Append("    ").Append(token.Name).Append(": ").Append(_overrides[token.Name]).AppendLine(";");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private void Restore(string stored)
    {
        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(stored);
            if (saved is null)
            {
                return;
            }

            foreach (var (name, value) in saved)
            {
                _overrides[name] = value;
            }
        }
        catch (JsonException)
        {
            // A hand-edited or half-written entry must not take the site down; the visitor simply
            // starts again from the shipped theme.
            _overrides.Clear();
        }
    }

    private async Task PushAsync(CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync(
            "omniShowcaseTheme.apply",
            cancellationToken,
            _overrides,
            Mode is ThemeMode.Dark ? "dark" : "light",
            StorageKey,
            JsonSerializer.Serialize(_overrides)).ConfigureAwait(false);
        Changed?.Invoke();
    }
}
