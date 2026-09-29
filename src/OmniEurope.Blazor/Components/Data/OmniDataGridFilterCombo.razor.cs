using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Free-text filter input backed by a list of suggestions. Unlike a native <c>datalist</c>, the list
/// is ordinary markup, so it can be styled and the typed fragment can be highlighted inside each
/// suggestion. Every keystroke raises <see cref="ValueChanged"/>: the grid filters as the user types.
/// <see cref="OmniComponentBase.Id"/> goes on the input, <see cref="OmniComponentBase.Class"/> and the
/// additional attributes on the outer element.
/// </summary>
public partial class OmniDataGridFilterCombo
{
    private int _activeIndex = -1;
    private bool _open;

    /// <summary>The typed text, which is the filter value.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Raised with the text at every keystroke, and with the suggestion picked.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>The candidates listed under the input, narrowed to those containing the typed text (case and accents ignored).</summary>
    [Parameter]
    public IReadOnlyList<string> Suggestions { get; set; } = [];

    /// <summary>Placeholder of the input.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Raised when a suggestion is chosen, as opposed to text being typed. The suggestion element is
    /// gone from the DOM by the time its click would bubble, so a host that reacts to a pick needs
    /// to hear about it from here rather than from the event.
    /// </summary>
    [Parameter]
    public EventCallback OnPick { get; set; }

    /// <summary>Maximum suggestions rendered at once, so a large catalogue stays usable.</summary>
    [Parameter]
    public int MaxSuggestions { get; set; } = 50;

    private string ListId => $"{Id}-options";

    private bool IsOpen => _open && Matches.Count > 0;

    private string? ActiveOptionId => IsOpen && _activeIndex >= 0 ? OptionId(_activeIndex) : null;

    private string OptionId(int index) => $"{ListId}-{index}";

    private string OptionClass(int index) => index == _activeIndex
        ? "omni-combo__option omni-combo__option--active"
        : "omni-combo__option";

    /// <summary>
    /// Suggestions containing what has been typed so far, ignoring case and accents, the rule that marks
    /// the match. An empty box lists everything, so the control also works as a plain picker.
    /// </summary>
    private IReadOnlyList<string> Matches => (string.IsNullOrEmpty(Value)
            ? Suggestions
            : Suggestions.Where(candidate => OmniTextMatch.Contains(candidate, Value)))
        .Take(Math.Max(1, MaxSuggestions))
        .ToArray();

    private void OpenList() => _open = true;

    private void CloseList()
    {
        _open = false;
        _activeIndex = -1;
    }

    private Task OnInputAsync(ChangeEventArgs args)
    {
        _open = true;
        _activeIndex = -1;
        return ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        var matches = Matches;
        switch (args.Key)
        {
            case "ArrowDown" when matches.Count > 0:
                _open = true;
                _activeIndex = _activeIndex + 1 >= matches.Count ? 0 : _activeIndex + 1;
                break;
            case "ArrowUp" when matches.Count > 0:
                _open = true;
                _activeIndex = _activeIndex <= 0 ? matches.Count - 1 : _activeIndex - 1;
                break;
            case "Enter" when IsOpen && _activeIndex >= 0 && _activeIndex < matches.Count:
                await PickAsync(matches[_activeIndex]);
                break;
            case "Escape":
                CloseList();
                break;
        }
    }

    private async Task PickAsync(string candidate)
    {
        CloseList();
        await ValueChanged.InvokeAsync(candidate);
        await OnPick.InvokeAsync();
    }

}
