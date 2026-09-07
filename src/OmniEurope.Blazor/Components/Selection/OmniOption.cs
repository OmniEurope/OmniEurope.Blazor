namespace OmniEurope.Blazor.Components;

public sealed record OmniOption<TValue>(TValue Value, string Text, bool Disabled = false, string? Group = null);
