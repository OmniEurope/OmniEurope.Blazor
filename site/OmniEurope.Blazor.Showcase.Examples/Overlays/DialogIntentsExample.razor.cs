using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>One dialog opened with each intention, its focus on the panel.</summary>
public partial class DialogIntentsExample
{
    private bool Opened { get; set; }

    private OmniTone Intent { get; set; }

    private void OpenIntent(OmniTone intent)
    {
        Intent = intent;
        Opened = true;
    }
}
