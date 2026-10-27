namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The four sample illustrations, in four proportions.</summary>
public enum SampleImage
{
    /// <summary>800 x 800: a pattern of coloured tiles.</summary>
    Square,

    /// <summary>600 x 900: a lighthouse under the stars.</summary>
    Portrait,

    /// <summary>1200 x 800: mountains by a lake.</summary>
    Landscape,

    /// <summary>256 x 256: an abstract emblem.</summary>
    Vector
}

/// <summary>The box an image is asked to fit, or none to let it keep its own size.</summary>
public enum ImageBox
{
    /// <summary>No width and no height: the image keeps its own size.</summary>
    Intrinsic,

    /// <summary>240 x 120.</summary>
    Landscape,

    /// <summary>120 x 200.</summary>
    Portrait,

    /// <summary>160 x 160.</summary>
    Square
}

/// <summary>
/// The sample illustrations as data addresses, so an example needs no file next to it: the SVG text is
/// escaped into the address, which any page whose policy allows data images can load.
/// </summary>
public static class SampleImages
{
    private const string Square = """
        <svg xmlns="http://www.w3.org/2000/svg" width="800" height="800" viewBox="0 0 800 800">
          <rect width="800" height="800" fill="#f5efe6"/>
          <rect x="0" y="0" width="200" height="200" fill="#e76f51"/><circle cx="200" cy="200" r="100" fill="#f4a261"/>
          <rect x="400" y="0" width="200" height="200" fill="#2a9d8f"/><path d="M600 0 L800 0 L800 200 Z" fill="#264653"/>
          <circle cx="100" cy="500" r="100" fill="#264653"/><rect x="200" y="400" width="200" height="200" fill="#e9c46a"/>
          <path d="M400 400 L600 600 L400 600 Z" fill="#e76f51"/><rect x="600" y="400" width="200" height="200" fill="#2a9d8f"/>
          <circle cx="700" cy="500" r="60" fill="#f5efe6"/><rect x="0" y="600" width="200" height="200" fill="#e9c46a"/>
          <path d="M200 800 A200 200 0 0 1 400 600 L400 800 Z" fill="#2a9d8f"/><rect x="400" y="600" width="200" height="200" fill="#264653"/>
          <circle cx="700" cy="700" r="100" fill="#e76f51"/><path d="M200 200 L400 200 L400 400 Z" fill="#264653"/>
          <circle cx="500" cy="300" r="80" fill="#e9c46a"/><rect x="0" y="200" width="100" height="200" fill="#2a9d8f"/>
        </svg>
        """;

    private const string Portrait = """
        <svg xmlns="http://www.w3.org/2000/svg" width="600" height="900" viewBox="0 0 600 900">
          <defs>
            <linearGradient id="night" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0b1026"/><stop offset="1" stop-color="#27427a"/></linearGradient>
            <linearGradient id="beam" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff3c4" stop-opacity="0.75"/><stop offset="1" stop-color="#fff3c4" stop-opacity="0"/></linearGradient>
          </defs>
          <rect width="600" height="900" fill="url(#night)"/>
          <g fill="#ffffff"><circle cx="80" cy="90" r="2"/><circle cx="160" cy="210" r="1.5"/><circle cx="420" cy="70" r="2.5"/><circle cx="520" cy="190" r="1.5"/><circle cx="300" cy="140" r="1.8"/><circle cx="470" cy="300" r="1.5"/><circle cx="60" cy="330" r="1.5"/></g>
          <path d="M312 300 L600 230 L600 370 Z" fill="url(#beam)"/>
          <path d="M0 900 L0 620 L180 590 L300 640 L380 610 L600 660 L600 900 Z" fill="#1b2340"/>
          <path d="M0 900 L0 760 Q150 720 300 770 T600 760 L600 900 Z" fill="#315a8c"/>
          <path d="M0 900 L0 830 Q150 800 300 840 T600 830 L600 900 Z" fill="#4a7ab0"/>
          <path d="M270 620 L285 330 L315 330 L330 620 Z" fill="#f1f1f1"/>
          <path d="M276 520 L324 520 L327 560 L273 560 Z M280 420 L320 420 L322 460 L278 460 Z" fill="#d6423a"/>
          <rect x="278" y="296" width="44" height="36" fill="#ffe28a"/>
          <path d="M270 296 L300 262 L330 296 Z" fill="#d6423a"/>
          <rect x="266" y="330" width="68" height="8" fill="#2b2b2b"/>
        </svg>
        """;

    private const string Landscape = """
        <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800">
          <defs>
            <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1d2b64"/><stop offset="0.55" stop-color="#f28f6b"/><stop offset="1" stop-color="#fcd9a8"/></linearGradient>
            <linearGradient id="lake" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f4a57d"/><stop offset="1" stop-color="#2c3e70"/></linearGradient>
          </defs>
          <rect width="1200" height="800" fill="url(#sky)"/>
          <circle cx="840" cy="390" r="70" fill="#ffe7b3"/>
          <path d="M0 470 L180 280 L300 390 L470 190 L640 400 L760 320 L930 470 Z" fill="#3b3f73"/>
          <path d="M470 190 L520 250 L495 245 L470 280 L450 240 L420 250 Z" fill="#e9ecf7"/>
          <path d="M520 470 L720 300 L860 410 L1010 250 L1200 430 L1200 470 Z" fill="#2a2d5c"/>
          <path d="M1010 250 L1050 290 L1030 288 L1010 312 L990 286 L970 292 Z" fill="#dfe3f5"/>
          <rect y="470" width="1200" height="330" fill="url(#lake)"/>
          <path d="M0 470 L180 620 L300 540 L470 700 L640 530 L760 590 L930 470 Z" fill="#3b3f73" opacity="0.35"/>
          <rect x="780" y="480" width="120" height="6" rx="3" fill="#ffe7b3" opacity="0.8"/>
          <rect x="760" y="505" width="160" height="5" rx="2.5" fill="#ffe7b3" opacity="0.55"/>
          <rect x="790" y="528" width="100" height="4" rx="2" fill="#ffe7b3" opacity="0.4"/>
          <path d="M0 800 L0 700 Q120 660 240 700 T480 705 L480 800 Z" fill="#1c2046"/>
          <path d="M60 700 l12 -60 l12 60 Z M100 705 l14 -80 l14 80 Z M150 700 l10 -48 l10 48 Z" fill="#141733"/>
        </svg>
        """;

    private const string Vector = """
        <svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">
          <circle cx="128" cy="128" r="120" fill="#1f2a44"/>
          <g fill="none" stroke-width="14" stroke-linecap="round">
            <path d="M128 40 C170 80 170 176 128 216" stroke="#5ec2b7"/>
            <path d="M128 40 C86 80 86 176 128 216" stroke="#f2a65a"/>
            <path d="M40 128 C80 86 176 86 216 128" stroke="#ef6f6c"/>
            <path d="M40 128 C80 170 176 170 216 128" stroke="#9f86c0"/>
          </g>
          <circle cx="128" cy="128" r="18" fill="#fdf6e3"/>
        </svg>
        """;

    /// <summary>Every sample, in declaration order.</summary>
    public static IReadOnlyList<SampleImage> All { get; } = [.. Enum.GetValues<SampleImage>()];

    /// <summary>Every box, in declaration order.</summary>
    public static IReadOnlyList<ImageBox> Boxes { get; } = [.. Enum.GetValues<ImageBox>()];

    /// <summary>The data address of a sample.</summary>
    public static string SourceOf(SampleImage sample) =>
        "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(sample switch
        {
            SampleImage.Portrait => Portrait,
            SampleImage.Landscape => Landscape,
            SampleImage.Vector => Vector,
            _ => Square
        });

    /// <summary>The text alternative of a sample, which an image always needs.</summary>
    public static string AltOf(SampleImage sample) => sample switch
    {
        SampleImage.Portrait => "Lighthouse on a cliff under a starry sky",
        SampleImage.Landscape => "Mountains by a lake at sunset",
        SampleImage.Vector => "Abstract flower-shaped emblem",
        _ => "Geometric pattern of coloured tiles"
    };

    /// <summary>The name of a sample, with its proportions.</summary>
    public static string LabelOf(SampleImage sample) => sample switch
    {
        SampleImage.Portrait => "Portrait (SVG 600 × 900)",
        SampleImage.Landscape => "Landscape (SVG 1200 × 800)",
        SampleImage.Vector => "Emblem (SVG 256 × 256)",
        _ => "Square (SVG 800 × 800)"
    };

    /// <summary>The width a box asks of an image, or null for none.</summary>
    public static int? WidthOf(ImageBox box) => box switch
    {
        ImageBox.Landscape => 240,
        ImageBox.Portrait => 120,
        ImageBox.Square => 160,
        _ => null
    };

    /// <summary>The height a box asks of an image, or null for none.</summary>
    public static int? HeightOf(ImageBox box) => box switch
    {
        ImageBox.Landscape => 120,
        ImageBox.Portrait => 200,
        ImageBox.Square => 160,
        _ => null
    };
}
