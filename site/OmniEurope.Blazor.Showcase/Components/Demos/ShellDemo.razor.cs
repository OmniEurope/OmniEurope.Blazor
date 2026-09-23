using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ShellDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
    private bool? SplashFound { get; set; }
    private static readonly string[] ScrollParagraphs =
    [
        "La colonne est plafonnée à 72rem et centrée ; le reste de la largeur appartient encore à l'élément principal.",
        "C'est donc lui qui défile, sur toute la largeur de la zone : sa barre se tient au bord droit du cadre.",
        "Un enfant à 100 % de hauteur remplirait exactement la zone ; un contenu plus haut la fait défiler.",
        "La marge du bas reste acquise à la fin du défilement.",
        "Réduire la fenêtre ramène la colonne à la largeur disponible, gouttière comprise."
    ];

    private bool SidebarOpen { get; set; } = true;
    private bool RightSidebarOpen { get; set; } = true;

    private OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    private bool RailOpen { get; set; }
}
