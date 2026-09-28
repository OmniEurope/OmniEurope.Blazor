namespace OmniEurope.Blazor.Components;

/// <summary>
/// The extensions of one <see cref="OmniHtmlEditor"/> taken together: the policy they merge into, the
/// toolbar they arrange, and their shortcuts, inline elements, context menu and table readers, in order.
/// Built again only when the editor's extensions change, so the merged policy stays the same instance
/// and its sanitiser is cached.
/// </summary>
internal sealed class HtmlEditorExtensionSet
{
    internal static HtmlEditorExtensionSet Empty { get; } = new([]);

    private HtmlEditorExtensionSet(IReadOnlyList<OmniHtmlEditorExtension> extensions)
    {
        Source = extensions;
        Policy = extensions.Aggregate((OmniHtmlSanitizerPolicy?)null, (policy, extension) => OmniHtmlSanitizerPolicy.Merge(policy, extension.SanitizerPolicy));
        Shortcuts = [.. extensions.SelectMany(extension => extension.Shortcuts)];
        InlineElements = [.. extensions.SelectMany(extension => extension.InlineElements)];
        ContextMenu = [.. extensions.SelectMany(extension => extension.ContextMenu)];
        TableReaders = [.. extensions.SelectMany(extension => extension.TableReaders)];
        var keys = Shortcuts.Select(shortcut => shortcut.Normalized).ToList();
        var duplicate = keys.GroupBy(key => key, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"The shortcut '{duplicate.Key}' of {nameof(OmniHtmlEditor)} is declared twice.");
        }

        ShortcutKeys = keys;
    }

    internal IReadOnlyList<OmniHtmlEditorExtension> Source { get; }

    /// <summary>Every extension's policy merged; null when none widens the allow-list.</summary>
    internal OmniHtmlSanitizerPolicy? Policy { get; }

    internal IReadOnlyList<OmniHtmlEditorShortcut> Shortcuts { get; }

    /// <summary>The combinations as the surface script matches them, in the order of <see cref="Shortcuts"/>.</summary>
    internal IReadOnlyList<string> ShortcutKeys { get; }

    internal IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements { get; }

    internal IReadOnlyList<OmniHtmlEditorCommand> ContextMenu { get; }

    internal IReadOnlyList<OmniHtmlEditorTableReader> TableReaders { get; }

    internal bool TracksSelection => Source.Any(extension => extension.TracksSelection)
        || ContextMenu.Any(command => command.Pressed is not null || command.Enabled is not null);

    internal static HtmlEditorExtensionSet For(IReadOnlyList<OmniHtmlEditorExtension>? extensions) =>
        extensions is null || extensions.Count == 0 ? Empty : new(extensions);

    /// <summary>
    /// Whether this set still describes the editor: the same extension instances in the same
    /// order. A parent that passes a new list holding the same extensions on every render keeps the set.
    /// </summary>
    internal bool Matches(IReadOnlyList<OmniHtmlEditorExtension>? extensions) =>
        Source.SequenceEqual(extensions ?? [], ReferenceEqualityComparer.Instance);

    internal IReadOnlyList<OmniHtmlEditorCommand> Arrange(IReadOnlyList<OmniHtmlEditorCommand> toolbar) =>
        Source.Aggregate(toolbar, (current, extension) => extension.ArrangeToolbar(current));

    /// <summary>Every command a shortcut may name: those of the toolbar, then those the extensions bring, then the context menu.</summary>
    internal OmniHtmlEditorCommand? Find(string name, IEnumerable<OmniHtmlEditorCommand> toolbar) =>
        toolbar.Concat(Source.SelectMany(extension => extension.Commands)).Concat(ContextMenu)
            .FirstOrDefault(command => command.Action != OmniHtmlEditorAction.Separator && string.Equals(command.Name, name, StringComparison.Ordinal));

    internal async Task NotifySelectionAsync(OmniHtmlEditorSelection selection)
    {
        foreach (var extension in Source.Where(extension => extension.TracksSelection))
        {
            await extension.OnSelectionChangedAsync(selection);
        }
    }
}
