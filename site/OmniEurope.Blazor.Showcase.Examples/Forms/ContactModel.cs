using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>
/// A model as an API would share it: standard attributes, without a message of their own.
/// <c>OmniDataAnnotationsValidator</c> writes the messages in the current culture.
/// </summary>
public sealed class ContactModel
{
    /// <summary>The required, length-checked name.</summary>
    [Required]
    [StringLength(40, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The address, checked for shape.</summary>
    [Required]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    /// <summary>The address typed a second time, compared with <see cref="Email"/>.</summary>
    [Required]
    [Compare(nameof(Email))]
    [Display(Name = "E-mail confirmation")]
    public string EmailConfirmation { get; set; } = string.Empty;

    /// <summary>The number of seats, bounded.</summary>
    [Range(1, 10)]
    [Display(Name = "Seats")]
    public int Seats { get; set; }
}
