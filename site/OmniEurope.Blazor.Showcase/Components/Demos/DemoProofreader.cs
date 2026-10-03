namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// A proofreader as small as a demonstration allows: one misspelled word it knows the correction of, and a word written
/// twice in a row, flagged as grammar. A real host plugs its own engine and dictionaries in the same place; the editor
/// only underlines, offers the corrections and calls back.
/// </summary>
internal sealed class DemoProofreader(string wrong, string right, string repeated) : OmniHtmlEditorProofreader
{
    private readonly HashSet<string> _accepted = new(StringComparer.OrdinalIgnoreCase);

    public override bool CanIgnoreAll => true;

    public override bool CanAddToDictionary => true;

    public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
        IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken)
    {
        var issues = new List<OmniHtmlEditorProofreadingIssue>();
        for (var index = 0; index < texts.Count; index++)
        {
            (int Start, string Word)? previous = null;
            foreach (var (start, word) in Words(texts[index].Text))
            {
                if (string.Equals(word, wrong, StringComparison.OrdinalIgnoreCase) && !_accepted.Contains(word))
                {
                    issues.Add(new(index, start, word.Length));
                }
                else if (previous is { } before && string.Equals(before.Word, word, StringComparison.OrdinalIgnoreCase)
                         && string.IsNullOrWhiteSpace(texts[index].Text[(before.Start + before.Word.Length)..start]))
                {
                    issues.Add(new(index, before.Start, start + word.Length - before.Start, OmniHtmlEditorProofreadingKind.Grammar, repeated));
                }

                previous = (start, word);
            }
        }

        return Task.FromResult<IReadOnlyList<OmniHtmlEditorProofreadingIssue>>(issues);
    }

    public override Task<IReadOnlyList<string>> SuggestAsync(
        OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken)
    {
        var passage = text.Text.Substring(issue.Start, issue.Length);
        IReadOnlyList<string> suggestions = issue.Kind == OmniHtmlEditorProofreadingKind.Grammar
            ? [Words(passage).First().Word]
            : [right];
        return Task.FromResult(suggestions);
    }

    public override Task IgnoreAllAsync(string passage, string? language)
    {
        _accepted.Add(passage);
        return Task.CompletedTask;
    }

    public override Task AddToDictionaryAsync(string passage, string? language)
    {
        _accepted.Add(passage);
        return Task.CompletedTask;
    }

    // The words of a text, letters only, with where each starts.
    private static IEnumerable<(int Start, string Word)> Words(string text)
    {
        var start = -1;
        for (var index = 0; index <= text.Length; index++)
        {
            var letter = index < text.Length && char.IsLetter(text[index]);
            if (letter && start < 0)
            {
                start = index;
            }
            else if (!letter && start >= 0)
            {
                yield return (start, text[start..index]);
                start = -1;
            }
        }
    }
}
