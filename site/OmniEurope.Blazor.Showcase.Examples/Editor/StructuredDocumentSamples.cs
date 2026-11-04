using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>
/// The document of the structured editor example: a service order with a customer, a delivery date, a priority chosen in a
/// list, payment terms chosen in a list that also takes free text, an amount, and a paragraph left for a signature. A real host
/// converts its own document (a word processing file) to these values; the editor knows no format.
/// </summary>
public static class StructuredDocumentSamples
{
    /// <summary>The style key the example maps to a bold class; a host's key is opaque to the editor.</summary>
    public const string EmphasisKey = "emphasis";

    /// <summary>A new instance of the service order each call, so that giving it to the editor starts the changes over.</summary>
    /// <returns>The document.</returns>
    public static OmniDocument ServiceOrder() => new(
    [
        new OmniDocumentBlock("order",
        [
            new OmniDocumentRun("order-title", "Service order", EmphasisKey),
            new OmniDocumentRun("order-for", " for "),
            new OmniDocumentField("order-customer", "customer", "Customer", OmniDocumentFieldKind.Text, "Northwind Logistics"),
            new OmniDocumentRun("order-delivery", ", to be delivered on "),
            new OmniDocumentField("order-date", "delivery-date", "Delivery date", OmniDocumentFieldKind.Date, "2026-11-04"),
            new OmniDocumentRun("order-end", ".")
        ]),
        new OmniDocumentBlock("terms",
        [
            new OmniDocumentRun("terms-priority", "Priority "),
            new OmniDocumentField("terms-priority-field", "priority", "Priority", OmniDocumentFieldKind.List, "Normal", ["Low", "Normal", "High"]),
            new OmniDocumentRun("terms-payment", ", payment "),
            new OmniDocumentField("terms-payment-field", "payment-terms", "Payment terms", OmniDocumentFieldKind.List, "Net 30", ["Net 30", "Net 60", "Prepaid"], AllowFreeText: true),
            new OmniDocumentRun("terms-total", ", total "),
            new OmniDocumentField("terms-total-field", "amount", "Amount", OmniDocumentFieldKind.Number, "1250.5"),
            new OmniDocumentRun("terms-currency", " EUR.")
        ]),
        new OmniDocumentBlock("signature",
        [
            new OmniDocumentRun("signature-label", "Signature of the carrier:")
        ])
    ]);
}
