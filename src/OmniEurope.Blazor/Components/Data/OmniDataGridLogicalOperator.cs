namespace OmniEurope.Blazor.Components;

/// <summary>How the two conditions of one column filter are combined (<see cref="OmniDataGridFilterMode.Advanced"/>).</summary>
public enum OmniDataGridLogicalOperator
{
    /// <summary>A row must match both conditions. The default.</summary>
    And,

    /// <summary>A row matches when it matches either condition.</summary>
    Or
}
