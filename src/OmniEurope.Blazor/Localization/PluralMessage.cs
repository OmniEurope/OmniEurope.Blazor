using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;

namespace OmniEurope.Blazor.Localization;

/// <summary>
/// A package text whose wording follows a number: the plural block of the ICU message syntax,
/// <c>{0, plural, one {# fichier} other {# fichiers}}</c>, inside an ordinary composite format string.
/// </summary>
/// <remarks>
/// The argument index picks the number, the language's rule (<see cref="PluralRules"/>) picks the form,
/// and <c>=N</c> names an exact number ahead of the rule. In a form, <c>#</c> stands for the number and
/// <c>{n}</c> placeholders keep their .NET meaning, format included. A text may hold several blocks and
/// ordinary placeholders around them. The rest of the ICU syntax (apostrophe quoting, <c>select</c>,
/// nested blocks, offsets) is not read.
/// </remarks>
internal static class PluralMessage
{
    private const string Keyword = "plural";

    /// <summary>
    /// A package text formatted with its arguments in the current cultures: through the plural reader when
    /// the text holds a plural block, through the localizer itself otherwise. Reading the raw text through
    /// the localizer keeps a host override in play, which may carry plural blocks of its own.
    /// </summary>
    internal static string Localize(IStringLocalizer localizer, string name, object[] arguments)
    {
        if (arguments.Length == 0)
        {
            return localizer[name].Value;
        }

        var text = localizer[name];
        return text.ResourceNotFound || !HasBlock(text.Value)
            ? localizer[name, arguments].Value
            : Format(text.Value, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, CultureInfo.CurrentCulture, arguments);
    }

    /// <summary>Whether <paramref name="text"/> holds at least one plural block.</summary>
    internal static bool HasBlock(string text) => Blocks(text).Count > 0;

    /// <summary>
    /// <paramref name="text"/> with each plural block replaced by the form of its number in
    /// <paramref name="language"/>, then formatted with <paramref name="arguments"/> like any composite
    /// format string.
    /// </summary>
    /// <exception cref="FormatException">A block names an argument that is not passed, or has no form to fall back on.</exception>
    internal static string Format(string text, string language, IFormatProvider culture, object[] arguments)
    {
        var blocks = Blocks(text);
        if (blocks.Count == 0)
        {
            return string.Format(culture, text, arguments);
        }

        var composite = new StringBuilder(text.Length);
        var position = 0;
        foreach (var block in blocks)
        {
            if (block.Argument >= arguments.Length)
            {
                throw new FormatException($"The plural block names argument {block.Argument}, and {arguments.Length} were passed.");
            }

            composite.Append(text, position, block.Start - position);
            composite.Append(WithNumber(Pick(block, language, arguments[block.Argument]), block.Argument));
            position = block.End;
        }

        composite.Append(text, position, text.Length - position);
        return string.Format(culture, composite.ToString(), arguments);
    }

    /// <summary>The plural blocks of a text, in order, each with its forms by selector (a category or <c>=N</c>).</summary>
    internal static IReadOnlyList<Block> Blocks(string text)
    {
        var blocks = new List<Block>();
        var index = 0;
        while ((index = text.IndexOf('{', index)) >= 0)
        {
            if (TryRead(text, index, out var block))
            {
                blocks.Add(block);
                index = block.End;
            }
            else
            {
                index++;
            }
        }

        return blocks;
    }

    private static string Pick(Block block, string language, object? value)
    {
        if (Whole(value) is { } count)
        {
            if (block.Forms.TryGetValue("=" + count.ToString(CultureInfo.InvariantCulture), out var exact))
            {
                return exact;
            }

            if (block.Forms.TryGetValue(PluralRules.Category(language, count), out var form))
            {
                return form;
            }
        }

        return block.Forms.TryGetValue(PluralRules.Other, out var other)
            ? other
            : throw new FormatException("A plural block needs an 'other' form.");
    }

    /// <summary>The whole number a plural block counts, or null when the argument is not one: the block then takes <c>other</c>.</summary>
    private static long? Whole(object? value) => value switch
    {
        PluralCount counted => counted.Value,
        sbyte or byte or short or ushort or int or uint or long => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ulong large => large > long.MaxValue ? long.MaxValue : (long)large,
        double real when real % 1 == 0 && Math.Abs(real) < long.MaxValue => (long)real,
        decimal exact when exact % 1 == 0 => WholeOf(exact),
        _ => null
    };

    // Out of the range of a long, the cast would throw: such a count takes the form of the largest.
    private static long WholeOf(decimal exact) => Math.Abs(exact) <= long.MaxValue ? (long)exact : long.MaxValue;

    /// <summary>The form with each <c>#</c> outside a placeholder turned into the placeholder of the counted argument.</summary>
    private static string WithNumber(string form, int argument)
    {
        var result = new StringBuilder(form.Length + 4);
        var depth = 0;
        foreach (var character in form)
        {
            depth += character == '{' ? 1 : character == '}' ? -1 : 0;
            if (character == '#' && depth == 0)
            {
                result.Append('{').Append(argument.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            else
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }

    /// <summary>Reads <c>{index, plural, selector {form} ...}</c> starting at the brace at <paramref name="start"/>.</summary>
    private static bool TryRead(string text, int start, out Block block)
    {
        block = default!;
        var cursor = start + 1;
        SkipSpaces(text, ref cursor);
        var digits = cursor;
        while (cursor < text.Length && char.IsAsciiDigit(text[cursor]))
        {
            cursor++;
        }

        if (!int.TryParse(text.AsSpan(digits, cursor - digits), NumberStyles.None, CultureInfo.InvariantCulture, out var argument)
            || !Expect(text, ref cursor, ',') || !ExpectKeyword(text, ref cursor) || !Expect(text, ref cursor, ','))
        {
            return false;
        }

        var forms = new Dictionary<string, string>(StringComparer.Ordinal);
        while (true)
        {
            SkipSpaces(text, ref cursor);
            if (cursor >= text.Length)
            {
                return false;
            }

            if (text[cursor] == '}')
            {
                block = new Block(start, cursor + 1, argument, forms);
                return forms.Count > 0;
            }

            var selector = cursor;
            while (cursor < text.Length && !char.IsWhiteSpace(text[cursor]) && text[cursor] != '{' && text[cursor] != '}')
            {
                cursor++;
            }

            var name = text[selector..cursor];
            SkipSpaces(text, ref cursor);
            if (name.Length == 0 || cursor >= text.Length || text[cursor] != '{')
            {
                return false;
            }

            var open = cursor;
            var depth = 0;
            do
            {
                depth += text[cursor] == '{' ? 1 : text[cursor] == '}' ? -1 : 0;
                cursor++;
            }
            while (depth > 0 && cursor < text.Length);

            if (depth != 0)
            {
                return false;
            }

            forms[name] = text[(open + 1)..(cursor - 1)];
        }
    }

    private static void SkipSpaces(string text, ref int cursor)
    {
        while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }
    }

    private static bool Expect(string text, ref int cursor, char expected)
    {
        SkipSpaces(text, ref cursor);
        if (cursor >= text.Length || text[cursor] != expected)
        {
            return false;
        }

        cursor++;
        return true;
    }

    private static bool ExpectKeyword(string text, ref int cursor)
    {
        SkipSpaces(text, ref cursor);
        if (string.CompareOrdinal(text, cursor, Keyword, 0, Keyword.Length) != 0)
        {
            return false;
        }

        cursor += Keyword.Length;
        return true;
    }

    /// <summary>One plural block: where it sits in the text, the argument it counts and its forms by selector.</summary>
    internal sealed record Block(int Start, int End, int Argument, IReadOnlyDictionary<string, string> Forms);
}
