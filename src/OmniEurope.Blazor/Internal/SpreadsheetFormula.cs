using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Reads and computes one formula, the text after its <c>=</c>, in a single recursive descent.
/// </summary>
/// <remarks>
/// Grammar, loosest binding first: <c>+</c> and <c>-</c>; <c>*</c> and <c>/</c>; <c>^</c>; a leading
/// sign; a trailing <c>%</c>; then a number (with a point), a quoted text, a cell reference
/// (<c>B3</c>, <c>$B$3</c>), a parenthesised expression or a function call. A range (<c>A1:B4</c>)
/// is only an argument: anywhere else it is <see cref="OmniSpreadsheetValue.ValueError"/>.
/// Arguments are separated by a comma or a semicolon, so a formula written with either spreadsheet
/// convention reads the same. Functions: SUM, AVERAGE, MIN, MAX, COUNT, ROUND and ABS, also under
/// their French names SOMME, MOYENNE, NB and ARRONDI.
/// </remarks>
internal sealed class SpreadsheetFormula
{
    private readonly string _text;
    private readonly SpreadsheetEvaluator _cells;
    private int _position;

    private SpreadsheetFormula(string text, SpreadsheetEvaluator cells)
    {
        _text = text;
        _cells = cells;
    }

    internal static OmniSpreadsheetValue Evaluate(string formula, SpreadsheetEvaluator cells)
    {
        var reader = new SpreadsheetFormula(formula, cells);
        try
        {
            var value = reader.Additive();
            reader.SkipSpaces();
            if (reader._position != reader._text.Length)
            {
                return Error(OmniSpreadsheetValue.SyntaxError);
            }

            // A formula pointing at an empty cell shows 0, as in every spreadsheet.
            return value.Kind == OmniSpreadsheetValueKind.Empty ? OmniSpreadsheetValue.FromNumber(0d) : value;
        }
        catch (FormatException)
        {
            return Error(OmniSpreadsheetValue.SyntaxError);
        }
    }

    // ---- expressions ----------------------------------------------------------------------------

    private OmniSpreadsheetValue Additive()
    {
        var left = Multiplicative();
        while (TryConsume('+', '-', out var symbol))
        {
            var right = Multiplicative();
            left = Arithmetic(left, right, symbol);
        }

        return left;
    }

    private OmniSpreadsheetValue Multiplicative()
    {
        var left = Power();
        while (TryConsume('*', '/', out var symbol))
        {
            var right = Power();
            left = Arithmetic(left, right, symbol);
        }

        return left;
    }

    private OmniSpreadsheetValue Power()
    {
        var left = Unary();
        while (TryConsume('^', '^', out _))
        {
            var right = Unary();
            left = Arithmetic(left, right, '^');
        }

        return left;
    }

    private OmniSpreadsheetValue Unary()
    {
        if (TryConsume('-', '+', out var sign))
        {
            var operand = Unary();
            return sign == '-' ? Arithmetic(OmniSpreadsheetValue.FromNumber(0d), operand, '-') : Arithmetic(operand, OmniSpreadsheetValue.FromNumber(0d), '+');
        }

        var value = Primary();
        while (TryConsume('%', '%', out _))
        {
            value = Arithmetic(value, OmniSpreadsheetValue.FromNumber(100d), '/');
        }

        return value;
    }

    private OmniSpreadsheetValue Primary()
    {
        SkipSpaces();
        if (_position >= _text.Length)
        {
            throw new FormatException();
        }

        var current = _text[_position];
        if (current == '(')
        {
            _position++;
            var inner = Additive();
            Expect(')');
            return inner;
        }

        if (char.IsAsciiDigit(current) || current == '.')
        {
            return OmniSpreadsheetValue.FromNumber(ReadNumber());
        }

        if (current == '"')
        {
            return OmniSpreadsheetValue.FromText(ReadQuoted());
        }

        if (!char.IsAsciiLetter(current) && current != '$')
        {
            throw new FormatException();
        }

        var name = ReadName();
        SkipSpaces();
        if (Peek() == '(')
        {
            _position++;
            return Call(name);
        }

        if (!SpreadsheetAddress.TryParse(name, out var row, out var column))
        {
            return Error(OmniSpreadsheetValue.NameError);
        }

        if (Peek() == ':')
        {
            // A range where a single value is expected: read it through, then refuse it.
            _position++;
            SkipSpaces();
            _ = ReadName();
            return Error(OmniSpreadsheetValue.ValueError);
        }

        return _cells.Contains(row, column) ? _cells.Evaluate(row, column) : Error(OmniSpreadsheetValue.ReferenceError);
    }

    // ---- functions ------------------------------------------------------------------------------

    /// <summary>One argument: a range, a lone reference (read like a one-cell range), or a value.</summary>
    private readonly record struct Argument(OmniSpreadsheetValue? Value, int Top, int Left, int Bottom, int Right)
    {
        internal bool IsRange => Value is null;
    }

    private OmniSpreadsheetValue Call(string name)
    {
        var arguments = new List<Argument>();
        SkipSpaces();
        if (Peek() == ')')
        {
            _position++;
        }
        else
        {
            while (true)
            {
                arguments.Add(ReadArgument());
                SkipSpaces();
                if (Peek() is ',' or ';')
                {
                    _position++;
                    continue;
                }

                Expect(')');
                break;
            }
        }

        return name.ToUpperInvariant() switch
        {
            "SUM" or "SOMME" => Aggregate(arguments, numbers => numbers.Count == 0 ? 0d : numbers.Sum()),
            "AVERAGE" or "MOYENNE" => Aggregate(arguments, numbers => numbers.Count == 0 ? double.NaN : numbers.Average(), divisionByZeroWhenEmpty: true),
            "MIN" => Aggregate(arguments, numbers => numbers.Count == 0 ? 0d : numbers.Min()),
            "MAX" => Aggregate(arguments, numbers => numbers.Count == 0 ? 0d : numbers.Max()),
            "COUNT" or "NB" => Aggregate(arguments, numbers => numbers.Count),
            "ABS" => Single(arguments, 1, values => Math.Abs(values[0])),
            "ROUND" or "ARRONDI" => Single(arguments, 2, values => Round(values[0], (int)Math.Clamp(Math.Truncate(values[1]), -15d, 15d))),
            _ => Error(OmniSpreadsheetValue.NameError)
        };
    }

    private Argument ReadArgument()
    {
        SkipSpaces();
        var start = _position;
        if (_position < _text.Length && (char.IsAsciiLetter(_text[_position]) || _text[_position] == '$'))
        {
            var first = ReadName();
            if (SpreadsheetAddress.TryParse(first, out var top, out var left))
            {
                SkipSpaces();
                if (Peek() == ':')
                {
                    _position++;
                    SkipSpaces();
                    if (!SpreadsheetAddress.TryParse(ReadName(), out var bottom, out var right))
                    {
                        throw new FormatException();
                    }

                    return new Argument(null, Math.Min(top, bottom), Math.Min(left, right), Math.Max(top, bottom), Math.Max(left, right));
                }

                if (Peek() is ',' or ';' or ')')
                {
                    return new Argument(null, top, left, top, left);
                }
            }

            _position = start;
        }

        return new Argument(Additive(), 0, 0, 0, 0);
    }

    /// <summary>
    /// The numbers the arguments hold. Inside a range, as in every spreadsheet, text and empty cells
    /// are skipped; a value typed straight into the call has to be a number. The first error met is
    /// the result.
    /// </summary>
    private OmniSpreadsheetValue Aggregate(List<Argument> arguments, Func<List<double>, double> reduce, bool divisionByZeroWhenEmpty = false)
    {
        var numbers = new List<double>();
        foreach (var argument in arguments)
        {
            if (argument.IsRange)
            {
                if (!_cells.Contains(argument.Top, argument.Left) || !_cells.Contains(argument.Bottom, argument.Right))
                {
                    return Error(OmniSpreadsheetValue.ReferenceError);
                }

                for (var row = argument.Top; row <= argument.Bottom; row++)
                {
                    for (var column = argument.Left; column <= argument.Right; column++)
                    {
                        var cell = _cells.Evaluate(row, column);
                        if (cell.IsError)
                        {
                            return cell;
                        }

                        if (cell.Kind == OmniSpreadsheetValueKind.Number)
                        {
                            numbers.Add(cell.Number);
                        }
                    }
                }

                continue;
            }

            var value = argument.Value!;
            if (value.Kind == OmniSpreadsheetValueKind.Empty)
            {
                continue;
            }

            if (!TryNumber(value, out var number, out var error))
            {
                return error;
            }

            numbers.Add(number);
        }

        return divisionByZeroWhenEmpty && numbers.Count == 0
            ? Error(OmniSpreadsheetValue.DivisionByZeroError)
            : OmniSpreadsheetValue.FromNumber(reduce(numbers));
    }

    /// <summary>A function of exactly <paramref name="count"/> numbers.</summary>
    private OmniSpreadsheetValue Single(List<Argument> arguments, int count, Func<double[], double> compute)
    {
        if (arguments.Count != count)
        {
            return Error(OmniSpreadsheetValue.ValueError);
        }

        var values = new double[count];
        for (var index = 0; index < count; index++)
        {
            var argument = arguments[index];
            OmniSpreadsheetValue value;
            if (argument.IsRange)
            {
                if (argument.Top != argument.Bottom || argument.Left != argument.Right)
                {
                    return Error(OmniSpreadsheetValue.ValueError);
                }

                if (!_cells.Contains(argument.Top, argument.Left))
                {
                    return Error(OmniSpreadsheetValue.ReferenceError);
                }

                value = _cells.Evaluate(argument.Top, argument.Left);
            }
            else
            {
                value = argument.Value!;
            }

            if (!TryNumber(value, out values[index], out var error))
            {
                return error;
            }
        }

        return OmniSpreadsheetValue.FromNumber(compute(values));
    }

    /// <summary>Rounds half away from zero; a negative number of digits rounds to tens, hundreds.</summary>
    private static double Round(double value, int digits)
    {
        if (digits >= 0)
        {
            return Math.Round(value, digits, MidpointRounding.AwayFromZero);
        }

        var factor = Math.Pow(10d, -digits);
        return Math.Round(value / factor, MidpointRounding.AwayFromZero) * factor;
    }

    // ---- arithmetic -----------------------------------------------------------------------------

    private static OmniSpreadsheetValue Arithmetic(OmniSpreadsheetValue left, OmniSpreadsheetValue right, char symbol)
    {
        if (!TryNumber(left, out var a, out var error) || !TryNumber(right, out var b, out error))
        {
            return error;
        }

        return symbol switch
        {
            '+' => OmniSpreadsheetValue.FromNumber(a + b),
            '-' => OmniSpreadsheetValue.FromNumber(a - b),
            '*' => OmniSpreadsheetValue.FromNumber(a * b),
            '/' => b == 0d ? Error(OmniSpreadsheetValue.DivisionByZeroError) : OmniSpreadsheetValue.FromNumber(a / b),
            _ => OmniSpreadsheetValue.FromNumber(Math.Pow(a, b))
        };
    }

    /// <summary>
    /// A value as a number: an empty cell is zero, a text that reads as a number is that number, any
    /// other text is <see cref="OmniSpreadsheetValue.ValueError"/> and an error stays itself.
    /// </summary>
    private static bool TryNumber(OmniSpreadsheetValue value, out double number, out OmniSpreadsheetValue error)
    {
        error = OmniSpreadsheetValue.Empty;
        number = 0d;
        switch (value.Kind)
        {
            case OmniSpreadsheetValueKind.Number:
                number = value.Number;
                return true;
            case OmniSpreadsheetValueKind.Empty:
                return true;
            case OmniSpreadsheetValueKind.Text when SpreadsheetEvaluator.TryReadNumber(value.Text, out number):
                return true;
            case OmniSpreadsheetValueKind.Error:
                error = value;
                return false;
            default:
                error = Error(OmniSpreadsheetValue.ValueError);
                return false;
        }
    }

    private static OmniSpreadsheetValue Error(string code) => OmniSpreadsheetValue.FromError(code);

    // ---- lexing ---------------------------------------------------------------------------------

    private char Peek() => _position < _text.Length ? _text[_position] : '\0';

    private void SkipSpaces()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
        {
            _position++;
        }
    }

    private bool TryConsume(char first, char second, out char symbol)
    {
        SkipSpaces();
        symbol = Peek();
        if (_position < _text.Length && (symbol == first || symbol == second))
        {
            _position++;
            return true;
        }

        return false;
    }

    private void Expect(char symbol)
    {
        SkipSpaces();
        if (Peek() != symbol)
        {
            throw new FormatException();
        }

        _position++;
    }

    private string ReadName()
    {
        var start = _position;
        while (_position < _text.Length && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] is '$' or '_' or '.'))
        {
            _position++;
        }

        return _text[start.._position];
    }

    private double ReadNumber()
    {
        var start = _position;
        while (_position < _text.Length && (char.IsAsciiDigit(_text[_position]) || _text[_position] == '.'))
        {
            _position++;
        }

        if (_position < _text.Length && _text[_position] is 'e' or 'E')
        {
            var mark = _position;
            _position++;
            if (_position < _text.Length && _text[_position] is '+' or '-')
            {
                _position++;
            }

            if (_position < _text.Length && char.IsAsciiDigit(_text[_position]))
            {
                while (_position < _text.Length && char.IsAsciiDigit(_text[_position]))
                {
                    _position++;
                }
            }
            else
            {
                _position = mark;
            }
        }

        return double.TryParse(_text.AsSpan(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new FormatException();
    }

    private string ReadQuoted()
    {
        _position++;
        var builder = new System.Text.StringBuilder();
        while (_position < _text.Length)
        {
            var current = _text[_position++];
            if (current != '"')
            {
                builder.Append(current);
                continue;
            }

            // A doubled quote is a quote inside the text.
            if (Peek() == '"')
            {
                builder.Append('"');
                _position++;
                continue;
            }

            return builder.ToString();
        }

        throw new FormatException();
    }
}
