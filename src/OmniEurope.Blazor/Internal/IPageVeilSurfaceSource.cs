namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the element of a page veil reads from its veil, wherever it is drawn (in place or by the main area
/// that holds the veil). The element holds its veil through this contract, so the veil, which builds the
/// element, and the element do not name each other.
/// </summary>
internal interface IPageVeilSurfaceSource
{
    /// <summary>True while the page waits: the element exists only then.</summary>
    bool Busy { get; }

    /// <summary>The attributes the host gave the veil, written on the element.</summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; }

    /// <summary>The id of the veil, written on the element.</summary>
    string? Id { get; }

    /// <summary>The element's classes.</summary>
    string RootClass { get; }

    /// <summary>Whether a logo is drawn, as markup or as an image address.</summary>
    bool HasLogo { get; }

    /// <summary>The logo as free markup; wins over <see cref="LogoSource"/>.</summary>
    RenderFragment? Logo { get; }

    /// <summary>The logo as an image address.</summary>
    string? LogoSource { get; }

    /// <summary>What is announced while the veil is up.</summary>
    string EffectiveLabel { get; }

    /// <summary>Raised when a parameter of the veil was set, so the element draws again.</summary>
    event Action? Changed;
}
