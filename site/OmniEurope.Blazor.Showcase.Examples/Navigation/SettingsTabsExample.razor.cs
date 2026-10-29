using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>A settings page in tabs, the last tab holding a danger zone whose actions ask first.</summary>
public partial class SettingsTabsExample : IDisposable
{
    private static readonly IReadOnlyList<OmniOption<string>> Languages = [new("en", "English"), new("fr", "Français")];

    private static readonly IReadOnlyList<OmniOption<string>> Modes =
    [
        new("system", "Follow the system"),
        new("light", "Light"),
        new("dark", "Dark")
    ];

    private SettingsTabsModel Model { get; } = new();

    // The danger zone asks through the confirmation dialog of the package, drawn by this example's own host.
    private OmniOverlayService Overlays { get; } = new();

    private string? Tab { get; set; } = "general";

    // The password typed in the dialog of "Delete the account": this page's own field, read by CanConfirm
    // and forgotten by OnConfirm and OnCancel. Demonstration only: it is checked for its length, not against anything.
    private string? AccountPassword { get; set; }

    private bool AccountPasswordIsFilled => AccountPassword is { Length: >= 8 };

    private void ForgetAccountPassword() => AccountPassword = null;

    // Demonstration only: nothing is deleted.
    private void DeleteAccount()
    {
        ForgetAccountPassword();
        Overlays.Notify("The account was deleted (not really).", OmniSeverity.Success);
    }

    // Demonstration only: nothing is deleted, the delay shows the busy button.
    private async Task DeleteProjectAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        Overlays.Notify("The project was deleted (not really).", OmniSeverity.Success);
    }

    // Demonstration only: the reset fails on purpose, to show OnConfirmError.
    private async Task ResetHistoryAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.8));
        throw new InvalidOperationException("The history could not be reset (on purpose).");
    }

    private void ReportResetFailure(Exception failure) => Overlays.Notify(failure.Message, OmniSeverity.Danger);

    /// <inheritdoc />
    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }
}
