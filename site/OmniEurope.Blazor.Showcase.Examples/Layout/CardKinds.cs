namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>What the header area of the preview card holds.</summary>
public enum CardHeaderKind
{
    /// <summary>No header: the card has no header area at all.</summary>
    None,

    /// <summary>A heading.</summary>
    Title,

    /// <summary>A heading and a badge.</summary>
    TitleAndBadge
}

/// <summary>What the footer area of the preview card holds.</summary>
public enum CardFooterKind
{
    /// <summary>No footer: the card has no footer area at all.</summary>
    None,

    /// <summary>A line of small text.</summary>
    Text,

    /// <summary>A Save button then a Cancel button.</summary>
    Actions
}
