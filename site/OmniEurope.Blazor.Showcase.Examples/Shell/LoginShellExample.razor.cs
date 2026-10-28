using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>A sign-in page in the login shell, with its optional sign-up line and the return address kept inside the application.</summary>
public partial class LoginShellExample
{
    /// <summary>Whether the sign-up line is drawn: without an address it is not.</summary>
    [Parameter]
    public bool ShowSignUp { get; set; } = true;

    /// <summary>The address the sign-up line points to.</summary>
    [Parameter]
    public string SignUpTarget { get; set; } = "components/workflows";

    private CredentialsModel Credentials { get; } = new();

    private string? Note { get; set; }

    private string? SignUpHref => ShowSignUp ? SignUpTarget : null;

    private void Submit() => Note = $"Form handed to the host for “{Credentials.Name}”.";

    /// <summary>What the sign-in form asks for.</summary>
    private sealed class CredentialsModel
    {
        public string Name { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;
    }
}
