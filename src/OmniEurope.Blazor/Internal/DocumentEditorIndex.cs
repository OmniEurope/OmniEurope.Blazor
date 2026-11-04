using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// A document of the editor once checked: its runs, fields and paragraphs by identifier, which is how the script and the
/// change set name them. Built once per document the host gives, and the one place the model is validated.
/// </summary>
internal sealed class DocumentEditorIndex
{
    /// <summary>The date format of a <see cref="OmniDocumentFieldKind.Date"/> value.</summary>
    internal const string DateFormat = "yyyy-MM-dd";

    /// <summary>The largest side of a drawing in pixels, the limit of <see cref="OmniInkCanvas.ExportPngAsync(int, int)"/>.</summary>
    private const int MaxDrawingSide = 8192;

    private DocumentEditorIndex()
    {
    }

    /// <summary>The runs by identifier.</summary>
    internal Dictionary<string, OmniDocumentRun> Runs { get; } = new(StringComparer.Ordinal);

    /// <summary>The fields by identifier.</summary>
    internal Dictionary<string, OmniDocumentField> Fields { get; } = new(StringComparer.Ordinal);

    /// <summary>The paragraphs by identifier.</summary>
    internal Dictionary<string, OmniDocumentBlock> Blocks { get; } = new(StringComparer.Ordinal);

    /// <summary>Checks <paramref name="document"/> and indexes it.</summary>
    /// <param name="document">The document the host gave.</param>
    /// <param name="parameterName">The parameter it came through, for the exception.</param>
    /// <returns>The index of the document.</returns>
    /// <exception cref="ArgumentException">The document breaks a rule of the model: a missing or duplicate identifier, a value that does not fit its field, a list without items, a drawing without a size.</exception>
    internal static DocumentEditorIndex Build(OmniDocument document, string parameterName)
    {
        var index = new DocumentEditorIndex();
        if (document.Blocks is null)
        {
            throw new ArgumentException($"{nameof(OmniDocument)}.{nameof(OmniDocument.Blocks)} is null.", parameterName);
        }

        foreach (var block in document.Blocks)
        {
            index.AddBlock(block, parameterName);
        }

        return index;
    }

    /// <summary>The size in pixels, at 96 dpi, a drawing sheet of the given size in points is exported at.</summary>
    /// <param name="points">The width or height in points.</param>
    /// <returns>The pixels, rounded and at least 1.</returns>
    internal static int Pixels(double points) => Math.Max(1, (int)Math.Round(points * 96d / 72d, MidpointRounding.AwayFromZero));

    /// <summary>The value as the model stores it: an empty string is no value.</summary>
    /// <param name="value">The value, or null.</param>
    /// <returns>The value, or null when it is null or empty.</returns>
    internal static string? Normalize(string? value) => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>Whether two values of a field are the same value: numbers by their number, anything else by their text.</summary>
    /// <param name="kind">The kind of the field.</param>
    /// <param name="left">A normalized value.</param>
    /// <param name="right">Another normalized value.</param>
    /// <returns>True when they are the same value.</returns>
    internal static bool SameValue(OmniDocumentFieldKind kind, string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return kind == OmniDocumentFieldKind.Number
            ? ParseNumber(left) == ParseNumber(right)
            : string.Equals(left, right, StringComparison.Ordinal);
    }

    /// <summary>Reads a date written <see cref="DateFormat"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="date">The date read.</param>
    /// <returns>True when the text is a valid date in that format.</returns>
    internal static bool TryParseDate(string value, out DateOnly date)
        => DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Reads a number written in the invariant culture, with an optional sign and decimal point.</summary>
    /// <param name="value">The text.</param>
    /// <param name="number">The number read.</param>
    /// <returns>True when the text is such a number.</returns>
    internal static bool TryParseNumber(string value, out decimal number)
        => decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number);

    private static decimal ParseNumber(string value)
        => decimal.Parse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    private static ArgumentException Invalid(string parameterName, string message) => new(message, parameterName);

    private static void EnsureId(string? id, string what, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Invalid(parameterName, $"{what} has an empty id.");
        }
    }

    private void AddBlock(OmniDocumentBlock? block, string parameterName)
    {
        if (block is null)
        {
            throw Invalid(parameterName, "The document holds a null block.");
        }

        EnsureId(block.Id, "A block", parameterName);
        if (!Blocks.TryAdd(block.Id, block))
        {
            throw Invalid(parameterName, $"The block id '{block.Id}' is used twice.");
        }

        if (block.Inlines is null)
        {
            throw Invalid(parameterName, $"The block '{block.Id}' has no inlines.");
        }

        foreach (var inline in block.Inlines)
        {
            AddInline(inline, block.Id, parameterName);
        }

        if (block.Drawing is { } drawing)
        {
            EnsureDrawing(drawing, block.Id, parameterName);
        }
    }

    private void AddInline(OmniDocumentInline? inline, string blockId, string parameterName)
    {
        switch (inline)
        {
            case OmniDocumentRun run:
                EnsureInlineId(run.Id, blockId, parameterName);
                if (run.Text is null)
                {
                    throw Invalid(parameterName, $"The run '{run.Id}' has no text (use an empty string).");
                }

                Runs.Add(run.Id, run);
                break;
            case OmniDocumentField field:
                EnsureInlineId(field.Id, blockId, parameterName);
                EnsureField(field, parameterName);
                Fields.Add(field.Id, field);
                break;
            default:
                throw Invalid(parameterName, $"The block '{blockId}' holds {(inline is null ? "a null inline" : $"an inline of type {inline.GetType().Name}")}; only {nameof(OmniDocumentRun)} and {nameof(OmniDocumentField)} are supported.");
        }
    }

    private void EnsureInlineId(string? id, string blockId, string parameterName)
    {
        EnsureId(id, $"An inline of the block '{blockId}'", parameterName);
        if (Runs.ContainsKey(id!) || Fields.ContainsKey(id!))
        {
            throw Invalid(parameterName, $"The inline id '{id}' is used twice.");
        }
    }

    private static void EnsureField(OmniDocumentField field, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(field.Tag))
        {
            throw Invalid(parameterName, $"The field '{field.Id}' has an empty tag.");
        }

        if (string.IsNullOrWhiteSpace(field.Label))
        {
            throw Invalid(parameterName, $"The field '{field.Id}' has an empty label.");
        }

        if (!Enum.IsDefined(field.Kind))
        {
            throw Invalid(parameterName, $"The field '{field.Id}' has an unknown kind ({(int)field.Kind}).");
        }

        if (field.Kind != OmniDocumentFieldKind.List)
        {
            if (field.Items is not null || field.AllowFreeText)
            {
                throw Invalid(parameterName, $"The field '{field.Id}' is not a list: Items and AllowFreeText belong to {nameof(OmniDocumentFieldKind.List)} fields.");
            }
        }
        else
        {
            EnsureItems(field, parameterName);
        }

        EnsureValue(field, parameterName);
    }

    private static void EnsureItems(OmniDocumentField field, string parameterName)
    {
        var items = field.Items ?? [];
        if (items.Count == 0 && !field.AllowFreeText)
        {
            throw Invalid(parameterName, $"The list field '{field.Id}' has no items and does not allow free text, so nothing can be chosen.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw Invalid(parameterName, $"The list field '{field.Id}' has an empty item.");
            }

            if (!seen.Add(item))
            {
                throw Invalid(parameterName, $"The list field '{field.Id}' lists the item '{item}' twice.");
            }
        }
    }

    private static void EnsureValue(OmniDocumentField field, string parameterName)
    {
        if (Normalize(field.Value) is not { } value)
        {
            return;
        }

        var fits = field.Kind switch
        {
            OmniDocumentFieldKind.Date => TryParseDate(value, out _),
            OmniDocumentFieldKind.Number => TryParseNumber(value, out _),
            OmniDocumentFieldKind.List => field.AllowFreeText || field.Items!.Contains(value, StringComparer.Ordinal),
            _ => true
        };
        if (!fits)
        {
            throw Invalid(parameterName, $"The value '{value}' of the {field.Kind} field '{field.Id}' is not {FitText(field)}.");
        }
    }

    private static string FitText(OmniDocumentField field) => field.Kind switch
    {
        OmniDocumentFieldKind.Date => $"a date written {DateFormat}",
        OmniDocumentFieldKind.Number => "a number in the invariant culture",
        _ => "one of its items"
    };

    private static void EnsureDrawing(OmniDocumentDrawing drawing, string blockId, string parameterName)
    {
        if (drawing.Strokes is null || drawing.Strokes.Any(stroke => stroke is null))
        {
            throw Invalid(parameterName, $"The drawing of the block '{blockId}' has no strokes list or holds a null stroke.");
        }

        if (!IsSide(drawing.WidthPt) || !IsSide(drawing.HeightPt))
        {
            throw Invalid(parameterName, $"The drawing of the block '{blockId}' must be a finite size above zero, at most {MaxDrawingSide} pixels at 96 dpi on each side (found {drawing.WidthPt} by {drawing.HeightPt} pt).");
        }
    }

    private static bool IsSide(double points) => double.IsFinite(points) && points > 0 && points * 96d / 72d <= MaxDrawingSide;
}
