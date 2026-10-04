using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Writes an <see cref="OmniTableExportDocument"/> as an Office Open XML workbook (<c>.xlsx</c>) with nothing
/// but the base library: a zip of the few SpreadsheetML parts a spreadsheet needs, so it runs in WebAssembly
/// too. One sheet named after the title, a bold frozen heading row with a filter, then one row per exported
/// row: numbers as numbers, dates as dates, booleans as booleans, everything else as inline text. No cell
/// ever holds a formula; a text that a spreadsheet would read as one on editing is marked as text.
/// </summary>
internal static class XlsxWriter
{
    private const string ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string MainNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string DocumentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly DateTime Epoch = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

    // Cell styles, by index in styles.xml: 0 plain, 1 bold heading, 2 date, 3 date and time, 4 text kept as text.
    private const int Heading = 1;
    private const int DateStyle = 2;
    private const int DateTimeStyle = 3;
    private const int QuotedText = 4;

    internal static byte[] Write(OmniTableExportDocument document)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Part(zip, "[Content_Types].xml", xml =>
            {
                xml.WriteStartElement("Types", ContentTypes);
                Element(xml, ContentTypes, "Default", ("Extension", "rels"), ("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
                Element(xml, ContentTypes, "Default", ("Extension", "xml"), ("ContentType", "application/xml"));
                Element(xml, ContentTypes, "Override", ("PartName", "/xl/workbook.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"));
                Element(xml, ContentTypes, "Override", ("PartName", "/xl/worksheets/sheet1.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"));
                Element(xml, ContentTypes, "Override", ("PartName", "/xl/styles.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"));
            });
            Part(zip, "_rels/.rels", xml => Relationships(xml, ("rId1", DocumentRelationships + "/officeDocument", "xl/workbook.xml")));
            Part(zip, "xl/_rels/workbook.xml.rels", xml => Relationships(xml,
                ("rId1", DocumentRelationships + "/worksheet", "worksheets/sheet1.xml"),
                ("rId2", DocumentRelationships + "/styles", "styles.xml")));
            Part(zip, "xl/workbook.xml", xml =>
            {
                xml.WriteStartElement("workbook", MainNamespace);
                xml.WriteAttributeString("xmlns", "r", null, RelationshipNamespace);
                xml.WriteStartElement("sheets", MainNamespace);
                xml.WriteStartElement("sheet", MainNamespace);
                xml.WriteAttributeString("name", SheetName(document.Title));
                xml.WriteAttributeString("sheetId", "1");
                xml.WriteAttributeString("id", RelationshipNamespace, "rId1");
            });
            Part(zip, "xl/styles.xml", Styles);
            Part(zip, "xl/worksheets/sheet1.xml", xml => Sheet(xml, document));
        }

        return buffer.ToArray();
    }

    private static void Sheet(XmlWriter xml, OmniTableExportDocument document)
    {
        var columns = document.Columns;
        var last = $"{ColumnName(Math.Max(1, columns.Count) - 1)}{document.Rows.Count + 1}";
        xml.WriteStartElement("worksheet", MainNamespace);
        xml.WriteStartElement("sheetViews", MainNamespace);
        xml.WriteStartElement("sheetView", MainNamespace);
        xml.WriteAttributeString("workbookViewId", "0");
        Element(xml, MainNamespace, "pane", ("ySplit", "1"), ("topLeftCell", "A2"), ("activePane", "bottomLeft"), ("state", "frozen"));
        xml.WriteEndElement();
        xml.WriteEndElement();
        if (columns.Count > 0)
        {
            xml.WriteStartElement("cols", MainNamespace);
            for (var index = 0; index < columns.Count; index++)
            {
                var widest = document.Rows.Select(row => index < row.Count ? row[index].Text.Length : 0).Append(columns[index].Title.Length).Max();
                var width = Math.Clamp(widest + 3, 8, 60).ToString(CultureInfo.InvariantCulture);
                var position = (index + 1).ToString(CultureInfo.InvariantCulture);
                Element(xml, MainNamespace, "col", ("min", position), ("max", position), ("width", width), ("customWidth", "1"));
            }

            xml.WriteEndElement();
        }

        xml.WriteStartElement("sheetData", MainNamespace);
        Row(xml, 1, columns.Select(column => new OmniTableExportCell(column.Title)).ToArray(), heading: true);
        for (var index = 0; index < document.Rows.Count; index++)
        {
            Row(xml, index + 2, document.Rows[index], heading: false);
        }

        xml.WriteEndElement();
        if (columns.Count > 0)
        {
            Element(xml, MainNamespace, "autoFilter", ("ref", $"A1:{last}"));
        }
    }

    private static void Row(XmlWriter xml, int number, IReadOnlyList<OmniTableExportCell> cells, bool heading)
    {
        xml.WriteStartElement("row", MainNamespace);
        xml.WriteAttributeString("r", number.ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];
            xml.WriteStartElement("c", MainNamespace);
            xml.WriteAttributeString("r", $"{ColumnName(index)}{number}");
            if (heading)
            {
                xml.WriteAttributeString("s", Heading.ToString(CultureInfo.InvariantCulture));
            }

            if (!heading && cell.Number is { } value)
            {
                xml.WriteElementString("v", MainNamespace, value.ToString(CultureInfo.InvariantCulture));
            }
            else if (!heading && cell.Date is { } date)
            {
                var clock = date.DateTime;
                xml.WriteAttributeString("s", (clock.TimeOfDay == TimeSpan.Zero ? DateStyle : DateTimeStyle).ToString(CultureInfo.InvariantCulture));
                xml.WriteElementString("v", MainNamespace, (clock - Epoch).TotalDays.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (!heading && cell.Boolean is { } flag)
            {
                xml.WriteAttributeString("t", "b");
                xml.WriteElementString("v", MainNamespace, flag ? "1" : "0");
            }
            else if (cell.Text.Length > 0)
            {
                xml.WriteAttributeString("t", "inlineStr");
                if (!heading && cell.Text.IndexOfAny(FormulaTriggers, 0, 1) == 0)
                {
                    xml.WriteAttributeString("s", QuotedText.ToString(CultureInfo.InvariantCulture));
                }

                xml.WriteStartElement("is", MainNamespace);
                xml.WriteStartElement("t", MainNamespace);
                xml.WriteAttributeString("xml", "space", null, "preserve");
                xml.WriteString(XmlText(cell.Text));
                xml.WriteEndElement();
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    private static void Styles(XmlWriter xml)
    {
        xml.WriteStartElement("styleSheet", MainNamespace);
        xml.WriteStartElement("numFmts", MainNamespace);
        xml.WriteAttributeString("count", "1");
        Element(xml, MainNamespace, "numFmt", ("numFmtId", "164"), ("formatCode", "yyyy-mm-dd hh:mm"));
        xml.WriteEndElement();
        xml.WriteStartElement("fonts", MainNamespace);
        xml.WriteAttributeString("count", "2");
        xml.WriteStartElement("font", MainNamespace);
        xml.WriteEndElement();
        xml.WriteStartElement("font", MainNamespace);
        Element(xml, MainNamespace, "b");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("fills", MainNamespace);
        xml.WriteAttributeString("count", "2");
        foreach (var pattern in new[] { "none", "gray125" })
        {
            xml.WriteStartElement("fill", MainNamespace);
            Element(xml, MainNamespace, "patternFill", ("patternType", pattern));
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
        xml.WriteStartElement("borders", MainNamespace);
        xml.WriteAttributeString("count", "1");
        Element(xml, MainNamespace, "border");
        xml.WriteEndElement();
        xml.WriteStartElement("cellStyleXfs", MainNamespace);
        xml.WriteAttributeString("count", "1");
        Element(xml, MainNamespace, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"));
        xml.WriteEndElement();
        xml.WriteStartElement("cellXfs", MainNamespace);
        xml.WriteAttributeString("count", "5");
        Element(xml, MainNamespace, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"));
        Element(xml, MainNamespace, "xf", ("numFmtId", "0"), ("fontId", "1"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"), ("applyFont", "1"));
        Element(xml, MainNamespace, "xf", ("numFmtId", "14"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"), ("applyNumberFormat", "1"));
        Element(xml, MainNamespace, "xf", ("numFmtId", "164"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"), ("applyNumberFormat", "1"));
        Element(xml, MainNamespace, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"), ("quotePrefix", "1"));
    }

    private static void Relationships(XmlWriter xml, params (string Id, string Type, string Target)[] relationships)
    {
        xml.WriteStartElement("Relationships", PackageRelationships);
        foreach (var (id, type, target) in relationships)
        {
            xml.WriteStartElement("Relationship", PackageRelationships);
            xml.WriteAttributeString("Id", id);
            xml.WriteAttributeString("Type", type);
            xml.WriteAttributeString("Target", target);
            xml.WriteEndElement();
        }
    }

    private static void Part(ZipArchive zip, string name, Action<XmlWriter> write)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        xml.WriteStartDocument(true);
        write(xml);
        xml.WriteEndDocument();
    }

    private static void Element(XmlWriter xml, string ns, string name, params (string Name, string Value)[] attributes)
    {
        xml.WriteStartElement(name, ns);
        foreach (var (attribute, value) in attributes)
        {
            xml.WriteAttributeString(attribute, value);
        }

        xml.WriteEndElement();
    }

    /// <summary>The letters of a zero-based column index: A to Z, then AA, AB...</summary>
    internal static string ColumnName(int index)
    {
        var name = string.Empty;
        for (var rest = index + 1; rest > 0; rest = (rest - 1) / 26)
        {
            name = (char)('A' + ((rest - 1) % 26)) + name;
        }

        return name;
    }

    /// <summary>A sheet name a spreadsheet accepts: no <c>[]:*?/\</c>, no edge apostrophe, 31 characters at most.</summary>
    internal static string SheetName(string title)
    {
        var name = new string(title.Where(character => "[]:*?/\\".IndexOf(character) < 0 && Allowed(character)).ToArray()).Trim().Trim('\'');
        name = name.Length > 31 ? name[..31].TrimEnd('\'') : name;
        return string.IsNullOrWhiteSpace(name) ? "Export" : name;
    }

    // XML 1.0 forbids most control characters: dropped rather than written as a broken part. Surrogate
    // pairs (emoji, rare scripts) are kept whole.
    private static string XmlText(string text) =>
        text.All(Allowed) ? text : new string(text.Where(Allowed).ToArray());

    private static bool Allowed(char character) =>
        character is '\t' or '\n' or '\r' || (character >= ' ' && character != '￾' && character != '￿');
}
