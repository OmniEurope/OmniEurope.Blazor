using System.Collections;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The one table an application keeps for a status type: which badge colour, label, icon and
/// explanation each value gets. Built once and shared by every <see cref="OmniStatusBadge{TValue}"/>
/// of that type, so the same value never reads healthy on one page and alarming on another.
/// </summary>
/// <typeparam name="TValue">The status type, usually an enumeration of the host.</typeparam>
/// <remarks>
/// A collection initializer fills it:
/// <code>
/// new OmniStatusMap&lt;RunState&gt;
/// {
///     { RunState.Succeeded, OmniBadgeVariant.Success, "Succeeded", OmniIconName.CheckCircle },
///     { RunState.Failed, OmniBadgeVariant.Danger, "Failed", OmniIconName.Error }
/// };
/// </code>
/// A value absent from the map is drawn with <see cref="Fallback"/>, or as a neutral badge carrying
/// the value's own text; a null value is drawn with <see cref="Empty"/>, or as a neutral dash.
/// </remarks>
public sealed class OmniStatusMap<TValue> : IEnumerable<KeyValuePair<TValue, OmniStatus>>
{
    private readonly List<KeyValuePair<TValue, OmniStatus>> _entries = [];
    private readonly IEqualityComparer<TValue> _comparer;

    /// <summary>Creates an empty map comparing values with <paramref name="comparer"/>, or the default comparer.</summary>
    public OmniStatusMap(IEqualityComparer<TValue>? comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<TValue>.Default;
    }

    /// <summary>
    /// Resolves <see cref="OmniStatus.Text"/> and <see cref="OmniStatus.Description"/> as resource keys
    /// of the host when set; null uses them as written.
    /// </summary>
    public IStringLocalizer? Localizer { get; init; }

    /// <summary>How a value absent from the map is drawn. Null draws it neutral, with the value's own text.</summary>
    public OmniStatus? Fallback { get; init; }

    /// <summary>How a null value is drawn. Null draws a neutral dash.</summary>
    public OmniStatus? Empty { get; init; }

    /// <summary>The number of mapped values.</summary>
    public int Count => _entries.Count;

    /// <summary>Maps <paramref name="value"/> to <paramref name="status"/>, replacing an earlier mapping of the same value.</summary>
    public OmniStatusMap<TValue> Add(TValue value, OmniStatus status)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(status);
        var index = _entries.FindIndex(entry => _comparer.Equals(entry.Key, value));
        if (index >= 0)
        {
            _entries[index] = new KeyValuePair<TValue, OmniStatus>(value, status);
        }
        else
        {
            _entries.Add(new KeyValuePair<TValue, OmniStatus>(value, status));
        }

        return this;
    }

    /// <summary>Maps <paramref name="value"/> to a badge of <paramref name="variant"/> labelled <paramref name="text"/>.</summary>
    public OmniStatusMap<TValue> Add(TValue value, OmniBadgeVariant variant, string text, OmniIconName? icon = null) =>
        Add(value, new OmniStatus(variant, text) { Icon = icon });

    /// <summary>The status mapped to <paramref name="value"/>, without falling back.</summary>
    public bool TryGet(TValue value, out OmniStatus status)
    {
        if (value is not null)
        {
            foreach (var entry in _entries)
            {
                if (_comparer.Equals(entry.Key, value))
                {
                    status = entry.Value;
                    return true;
                }
            }
        }

        status = null!;
        return false;
    }

    /// <summary>
    /// The status drawn for <paramref name="value"/>: its mapping, else <see cref="Fallback"/>, else a
    /// neutral badge with the value's text; <see cref="Empty"/> or a neutral dash for null.
    /// </summary>
    public OmniStatus Resolve(TValue? value)
    {
        if (value is null)
        {
            return Empty ?? new OmniStatus(OmniBadgeVariant.Neutral, "-");
        }

        if (TryGet(value, out var status))
        {
            return status;
        }

        return Fallback ?? new OmniStatus(OmniBadgeVariant.Neutral, value.ToString() ?? string.Empty);
    }

    /// <summary>A text of a status as it is shown: through <see cref="Localizer"/> when the map has one.</summary>
    public string Localize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return Localizer is null ? text : Localizer[text].Value;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TValue, OmniStatus>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
