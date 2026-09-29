using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// A model whose rules are DataAnnotations, as a model shared with an API usually is. The attributes
/// carry no message: <c>OmniDataAnnotationsValidator</c> writes them in the current culture. The display
/// names are showcase resource keys, which the validator resolves through its <c>Localizer</c>.
/// </summary>
public sealed class AnnotatedValidationDemoModel
{
    /// <summary>The required, length-checked project name.</summary>
    [Required]
    [StringLength(40, MinimumLength = 2)]
    [Display(Name = "DemoAnnotatedValidationName")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The owner's address, checked for shape.</summary>
    [Required]
    [EmailAddress]
    [Display(Name = "DemoAnnotatedValidationMail")]
    public string Mail { get; set; } = string.Empty;

    /// <summary>The address typed a second time, compared with <see cref="Mail"/>.</summary>
    [Required]
    [Compare(nameof(Mail))]
    [Display(Name = "DemoAnnotatedValidationMailConfirmation")]
    public string MailConfirmation { get; set; } = string.Empty;

    /// <summary>The number of replicas, bounded.</summary>
    [Range(1, 10)]
    [Display(Name = "DemoAnnotatedValidationReplicas")]
    public int Replicas { get; set; }
}
