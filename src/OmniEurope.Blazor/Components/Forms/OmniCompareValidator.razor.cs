namespace OmniEurope.Blazor.Components;

public partial class OmniCompareValidator<TValue>
{
    private Func<TValue>? _otherAccessor;

    [Parameter, EditorRequired]
    public Expression<Func<TValue>> Other { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = string.Empty;

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("CompareValidatorMessage")
        : Message;

    protected override void OnParametersSet()
    {
        _otherAccessor = Other.Compile();
        base.OnParametersSet();
    }

    protected override string? GetValidationError(TValue value) =>
        EqualityComparer<TValue>.Default.Equals(value, _otherAccessor!()) ? null : EffectiveMessage;
}
