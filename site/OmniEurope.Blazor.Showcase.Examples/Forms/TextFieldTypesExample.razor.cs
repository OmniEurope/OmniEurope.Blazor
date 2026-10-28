namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>The single-line box in each of its types.</summary>
public partial class TextFieldTypesExample
{
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private string Text { get; set; } = string.Empty;

    private string Email { get; set; } = string.Empty;

    private string Telephone { get; set; } = string.Empty;

    private string Address { get; set; } = "https://example.com";

    private string Search { get; set; } = string.Empty;

    private string Password { get; set; } = string.Empty;

    private void Generate() =>
        Password = System.Security.Cryptography.RandomNumberGenerator.GetString(PasswordAlphabet, 16);
}
