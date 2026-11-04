using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>
/// The pages of the document viewer example: a scanned cover sheet drawn as a picture, and a contract as HTML with
/// fields. A real host converts its own document (a word processing file, a PDF) to these pages; the viewer knows no format.
/// </summary>
public static class DocumentViewerSamples
{
    // A4 at 72 points to the inch.
    private const double A4WidthPt = 595.3;
    private const double A4HeightPt = 841.9;

    private const string CoverSheet = """
        <svg xmlns="http://www.w3.org/2000/svg" width="595" height="842" viewBox="0 0 595 842">
          <rect width="595" height="842" fill="#fbfaf6"/>
          <rect x="48" y="56" width="180" height="34" rx="4" fill="#2a3b5f"/>
          <text x="62" y="79" font-family="Arial, sans-serif" font-size="18" fill="#ffffff">Northwind Logistics</text>
          <text x="48" y="168" font-family="Arial, sans-serif" font-size="30" font-weight="700" fill="#1f2a44">Delivery file</text>
          <text x="48" y="198" font-family="Arial, sans-serif" font-size="15" fill="#5b6475">Scanned cover sheet, file 2026-0417</text>
          <g fill="#c9ced8">
            <rect x="48" y="260" width="499" height="8" rx="4"/><rect x="48" y="284" width="470" height="8" rx="4"/>
            <rect x="48" y="308" width="499" height="8" rx="4"/><rect x="48" y="332" width="410" height="8" rx="4"/>
            <rect x="48" y="380" width="499" height="8" rx="4"/><rect x="48" y="404" width="486" height="8" rx="4"/>
            <rect x="48" y="428" width="440" height="8" rx="4"/><rect x="48" y="452" width="499" height="8" rx="4"/>
            <rect x="48" y="476" width="300" height="8" rx="4"/>
          </g>
          <rect x="350" y="640" width="190" height="96" rx="6" fill="none" stroke="#b42318" stroke-width="3" transform="rotate(-6 445 688)"/>
          <text x="372" y="694" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#b42318" transform="rotate(-6 445 688)">RECEIVED</text>
        </svg>
        """;

    // The HTML of the contract page, as a converter would write it. It also carries what a hostile or careless file could
    // carry (a script, an event handler, a javascript: link): the viewer removes them, so none of them reaches the page.
    private const string ContractHtml = """
        <h1>Delivery contract</h1>
        <p>Between <b data-oe-field="supplier.name">Northwind Logistics</b> and <b data-oe-field="customer.name">Contoso Ltd</b>,
        signed on <span data-oe-field="contract.date">4 November 2026</span>, the supplier delivers the goods listed below.</p>
        <table>
          <thead><tr><th>Goods</th><th>Quantity</th><th>Price</th></tr></thead>
          <tbody>
            <tr><td>Pallet of crates</td><td data-oe-field="line1.quantity">12</td><td data-oe-field="line1.price">480.00 EUR</td></tr>
            <tr><td>Cold storage, per week</td><td data-oe-field="line2.quantity">3</td><td data-oe-field="line2.price">150.00 EUR</td></tr>
          </tbody>
        </table>
        <p>Total due: <b data-oe-field="contract.total">630.00 EUR</b>, payable within <span data-oe-field="payment.terms">30 days</span>.</p>
        <p onclick="alert('this handler is removed')">The carrier signs the delivery note on arrival.</p>
        <p><a href="javascript:alert('this address is removed')">Read the general terms</a></p>
        <script>document.title = 'this script is removed';</script>
        """;

    /// <summary>The cover sheet as the address of a picture, the way a host serving its own images would give it.</summary>
    public static string CoverSheetAddress { get; } = "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(CoverSheet);

    /// <summary>The pages of the document: the cover sheet as a picture, then the contract as HTML.</summary>
    public static IReadOnlyList<OmniDocumentPage> Pages { get; } =
    [
        new(1, ImageUrl: CoverSheetAddress, WidthPt: A4WidthPt, HeightPt: A4HeightPt),
        new(2, Html: ContractHtml, WidthPt: A4WidthPt, HeightPt: A4HeightPt)
    ];

    /// <summary>What the tooltip of each field says, by the tag of its <c>data-oe-field</c> attribute.</summary>
    public static IReadOnlyDictionary<string, string> FieldLabels { get; } = new Dictionary<string, string>
    {
        ["supplier.name"] = "Supplier",
        ["customer.name"] = "Customer",
        ["contract.date"] = "Signature date",
        ["line1.quantity"] = "Quantity, line 1",
        ["line1.price"] = "Price, line 1",
        ["line2.quantity"] = "Quantity, line 2",
        ["line2.price"] = "Price, line 2",
        ["contract.total"] = "Total amount",
        ["payment.terms"] = "Payment terms"
    };
}
