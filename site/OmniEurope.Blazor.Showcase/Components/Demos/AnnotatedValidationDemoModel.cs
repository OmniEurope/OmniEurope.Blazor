using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// A model whose rules are DataAnnotations, as a model shared with an API usually is. The attributes
/// carry no message: <c>OmniDataAnnotationsValidator</c> writes them in the current culture.
/// </summary>
public sealed class AnnotatedValidationDemoModel
{
    /// <summary>The required, length-checked project name.</summary>
    [Required]
    [StringLength(40, MinimumLength = 2)]
    [Display(Name = "Nom du projet")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The owner's address, checked for shape.</summary>
    [Required]
    [EmailAddress]
    [Display(Name = "Courriel du responsable")]
    public string Mail { get; set; } = string.Empty;

    /// <summary>The number of replicas, bounded.</summary>
    [Range(1, 10)]
    [Display(Name = "Réplicas")]
    public int Replicas { get; set; }
}
