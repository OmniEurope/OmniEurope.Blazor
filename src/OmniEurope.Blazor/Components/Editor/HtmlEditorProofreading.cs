using System.Text;
using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The proofreaders of an <see cref="OmniHtmlEditor"/>'s extensions as the surface script uses them: the blocks it
/// sends are checked by each proofreader, and the passage a right-click lands on gets its corrections and its
/// actions in the context menu. Answers travel as plain JSON written here, so nothing depends on reflection.
/// </summary>
internal sealed class HtmlEditorProofreading(OmniHtmlEditor owner) : IDisposable
{
    /// <summary>The most corrections the menu shows.</summary>
    internal const int MaxSuggestions = 5;

    // Cancelled when the editor goes: a check still running is no longer needed.
    private readonly CancellationTokenSource _life = new();

    private CancellationTokenSource? _menu;

    /// <summary>The passage the open menu is about, or null.</summary>
    internal ProofreadingMenuIssue? Issue { get; private set; }

    /// <summary>The corrections of <see cref="Issue"/>, best first; empty while they load or when there are none.</summary>
    internal IReadOnlyList<string> Suggestions { get; private set; } = [];

    /// <summary>Whether the corrections of <see cref="Issue"/> are still being asked for.</summary>
    internal bool Loading { get; private set; }

    /// <summary>The proofreader of <see cref="Issue"/>.</summary>
    internal OmniHtmlEditorProofreader? Proofreader =>
        Issue is { } issue && issue.Proofreader < owner.ExtensionSet.Proofreaders.Count ? owner.ExtensionSet.Proofreaders[issue.Proofreader] : null;

    /// <summary>
    /// Checks the blocks with every proofreader, as JSON rows <c>[proofreader, text, start, length, kind, message]</c>.
    /// A proofreader that throws gives nothing; the others still answer.
    /// </summary>
    internal async Task<string> CheckAsync(string[] texts, string?[] languages)
    {
        var proofreaders = owner.ExtensionSet.Proofreaders;
        if (proofreaders.Count == 0 || texts.Length == 0 || owner.IsLocked || owner.CurrentMode != OmniHtmlEditorMode.Visual)
        {
            return "[]";
        }

        var blocks = texts.Select((text, index) => new OmniHtmlEditorProofreadingText(text ?? string.Empty, index < languages.Length ? languages[index] : null)).ToList();
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            for (var index = 0; index < proofreaders.Count; index++)
            {
                IReadOnlyList<OmniHtmlEditorProofreadingIssue> issues;
                try
                {
                    issues = await proofreaders[index].CheckAsync(blocks, _life.Token);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    continue;
                }

                foreach (var issue in issues.Where(issue => issue.TextIndex >= 0 && issue.TextIndex < blocks.Count && issue.Length > 0))
                {
                    writer.WriteStartArray();
                    writer.WriteNumberValue(index);
                    writer.WriteNumberValue(issue.TextIndex);
                    writer.WriteNumberValue(issue.Start);
                    writer.WriteNumberValue(issue.Length);
                    writer.WriteNumberValue((int)issue.Kind);
                    if (issue.Message is null)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        writer.WriteStringValue(issue.Message);
                    }

                    writer.WriteEndArray();
                }
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// The menu opens on the passage the script describes (null: on no passage); its corrections are then asked for
    /// by <see cref="LoadSuggestionsAsync"/>. Whether the menu has a passage.
    /// </summary>
    internal bool Begin(string? issue)
    {
        Cancel();
        Issue = ProofreadingMenuIssue.Parse(issue);
        Suggestions = [];
        if (Issue is not null && Proofreader is null)
        {
            Issue = null;
        }

        Loading = Issue is not null;
        return Issue is not null;
    }

    /// <summary>Asks the proofreader of the menu's passage for its corrections; the caller renders them.</summary>
    internal async Task LoadSuggestionsAsync()
    {
        if (Issue is not { } opened || Proofreader is not { } proofreader)
        {
            return;
        }

        var cancellation = _menu = new CancellationTokenSource();
        try
        {
            var suggestions = await proofreader.SuggestAsync(
                new OmniHtmlEditorProofreadingText(opened.Text, opened.Language),
                new OmniHtmlEditorProofreadingIssue(0, opened.Start, opened.Length, opened.Kind, opened.Message),
                cancellation.Token);
            if (ReferenceEquals(Issue, opened))
            {
                Suggestions = [.. suggestions.Where(suggestion => !string.IsNullOrEmpty(suggestion)).Distinct(StringComparer.Ordinal).Take(MaxSuggestions)];
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A proofreader that fails to suggest leaves the menu with its other entries.
        }
        finally
        {
            if (ReferenceEquals(Issue, opened))
            {
                Loading = false;
            }
        }
    }

    /// <summary>The menu closed or moved on: a correction still being asked for is no longer wanted.</summary>
    internal void Cancel()
    {
        if (_menu is { } cancellation)
        {
            _menu = null;
            cancellation.Cancel();
            cancellation.Dispose();
        }

        Loading = false;
    }

    /// <summary>"Ignore all" or "Add to dictionary": the proofreader records the passage, then every block is checked again.</summary>
    internal async Task RecordAsync(bool dictionary)
    {
        if (Issue is not { } issue || Proofreader is not { } proofreader)
        {
            return;
        }

        Issue = null;
        try
        {
            if (dictionary)
            {
                await proofreader.AddToDictionaryAsync(issue.Passage, issue.Language);
            }
            else
            {
                await proofreader.IgnoreAllAsync(issue.Passage, issue.Language);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Nothing was recorded: the passage stays underlined.
        }
    }

    /// <summary>The menu's passage is done with (a correction typed, ignored once).</summary>
    internal void Forget() => Issue = null;

    public void Dispose()
    {
        _life.Cancel();
        _life.Dispose();
        _menu?.Dispose();
    }
}

/// <summary>The passage a context menu opened on, as the surface script describes it.</summary>
internal sealed record ProofreadingMenuIssue(int Proofreader, string Text, string? Language, int Start, int Length, OmniHtmlEditorProofreadingKind Kind, string? Message)
{
    /// <summary>The flagged text itself.</summary>
    internal string Passage => Text.Substring(Start, Length);

    internal static ProofreadingMenuIssue? Parse(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var text = root.GetProperty("text").GetString() ?? string.Empty;
            var start = root.GetProperty("s").GetInt32();
            var length = root.GetProperty("l").GetInt32();
            var kind = root.GetProperty("k").GetInt32();
            if (start < 0 || length <= 0 || start + length > text.Length || !Enum.IsDefined(typeof(OmniHtmlEditorProofreadingKind), kind))
            {
                return null;
            }

            return new(
                root.GetProperty("p").GetInt32(),
                text,
                root.TryGetProperty("lang", out var language) && language.ValueKind == JsonValueKind.String ? language.GetString() : null,
                start,
                length,
                (OmniHtmlEditorProofreadingKind)kind,
                root.TryGetProperty("m", out var message) && message.ValueKind == JsonValueKind.String ? message.GetString() : null);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}
