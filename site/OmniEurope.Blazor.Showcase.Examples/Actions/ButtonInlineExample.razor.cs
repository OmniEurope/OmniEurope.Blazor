namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A sentence whose words are buttons set inline.</summary>
public partial class ButtonInlineExample
{
    private string Word { get; set; } = "none";

    private void Pick(string word) => Word = word;
}
