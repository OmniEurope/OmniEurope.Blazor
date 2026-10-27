namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A long dialog that scrolls and a strict one that only its own button closes.</summary>
public partial class DialogVariantsExample
{
    private static readonly string[] Paragraphs =
    [
        "Every file is registered on the day it is received, whatever the channel it came through.",
        "A file missing a document is put on hold, and the applicant is told what is missing within five working days.",
        "Review follows the order of arrival, except for files marked urgent by the head of department.",
        "The decision is drafted by the reviewer and signed by a second person who did not take part in the review.",
        "The applicant is notified by post, and by email when an address was given.",
        "Closed files are archived for ten years, then destroyed unless a dispute is pending."
    ];

    private bool LongOpen { get; set; }

    private bool StrictOpen { get; set; }

    private int Acknowledgements { get; set; }

    private void Acknowledge()
    {
        Acknowledgements++;
        StrictOpen = false;
    }
}
