using System.IO;
using System.Text;
using System.Xml;

namespace Aero.Common.Xml;

/// <summary>
/// Extended XmlTextWriter with additional helper methods for common XML writing operations
/// </summary>
public class MyXmlWriter : XmlTextWriter
{
    public MyXmlWriter(Stream w, Encoding encoding) : base(w, encoding) { }
    public MyXmlWriter(string filename, Encoding encoding) : base(filename, encoding) { }
    public MyXmlWriter(TextWriter w) : base(w) { }

    /// <summary>
    /// Writes an element with a single attribute and value
    /// </summary>
    public void WriteAttributeElement(string element, string ns, string attribute, string value)
    {
        this.WriteStartElement(element, ns);
        this.WriteAttributeString(attribute, value);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with a single attribute (no namespace)
    /// </summary>
    public void WriteAttributeElement(string element, string attribute, string value)
    {
        this.WriteStartElement(element);
        this.WriteAttributeString(attribute, value);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a complete element with value (convenience method)
    /// </summary>
    public void WriteElement(string element, string value)
    {
        this.WriteStartElement(element);
        this.WriteString(value ?? string.Empty);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a complete element with value and namespace
    /// </summary>
    public void WriteElement(string element, string ns, string value)
    {
        this.WriteStartElement(element, ns);
        this.WriteString(value ?? string.Empty);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a complete element with multiple attributes
    /// </summary>
    public void WriteElement(string element, Dictionary<string, string> attributes, string value)
    {
        this.WriteStartElement(element);
        foreach (var attr in attributes)
        {
            this.WriteAttributeString(attr.Key, attr.Value);
        }
        if (!string.IsNullOrEmpty(value))
        {
            this.WriteString(value);
        }
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a CDATA section
    /// </summary>
    public void WriteCDataElement(string element, string cdataContent)
    {
        this.WriteStartElement(element);
        this.WriteCData(cdataContent ?? string.Empty);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a comment element
    /// </summary>
    public void WriteCommentElement(string comment)
    {
        this.WriteComment(comment ?? string.Empty);
    }

    /// <summary>
    /// Writes an element with boolean value
    /// </summary>
    public void WriteElement(string element, bool value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString().ToLowerInvariant());
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with integer value
    /// </summary>
    public void WriteElement(string element, int value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString());
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with long value
    /// </summary>
    public void WriteElement(string element, long value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString());
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with decimal value
    /// </summary>
    public void WriteElement(string element, decimal value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with double value
    /// </summary>
    public void WriteElement(string element, double value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with DateTime value (ISO 8601 format)
    /// </summary>
    public void WriteElement(string element, DateTime value)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString("O")); // ISO 8601 format
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with DateTime value using specified format
    /// </summary>
    public void WriteElement(string element, DateTime value, string format)
    {
        this.WriteStartElement(element);
        this.WriteString(value.ToString(format));
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an empty element
    /// </summary>
    public void WriteEmptyElement(string element)
    {
        this.WriteStartElement(element);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an empty element with attributes
    /// </summary>
    public void WriteEmptyElement(string element, Dictionary<string, string> attributes)
    {
        this.WriteStartElement(element);
        foreach (var attr in attributes)
        {
            this.WriteAttributeString(attr.Key, attr.Value);
        }
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes a namespace declaration
    /// </summary>
    public void WriteNamespaceDeclaration(string prefix, string uri)
    {
        this.WriteAttributeString("xmlns", prefix, null, uri);
    }

    /// <summary>
    /// Writes raw XML content
    /// </summary>
    public void WriteRawElement(string element, string rawXml)
    {
        this.WriteStartElement(element);
        this.WriteRaw(rawXml ?? string.Empty);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element with inner XML content
    /// </summary>
    public void WriteElementWithInnerXml(string element, string innerXml)
    {
        this.WriteStartElement(element);
        this.WriteRaw(innerXml ?? string.Empty);
        this.WriteEndElement();
    }

    /// <summary>
    /// Writes an element only if the value is not null or empty
    /// </summary>
    public void WriteElementIfNotEmpty(string element, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            WriteElement(element, value);
        }
    }

    /// <summary>
    /// Writes an element only if the value has a value (for nullable types)
    /// </summary>
    public void WriteElementIfHasValue(string element, int? value)
    {
        if (value.HasValue)
        {
            WriteElement(element, value.Value);
        }
    }

    /// <summary>
    /// Writes an element only if the value has a value (for nullable types)
    /// </summary>
    public void WriteElementIfHasValue(string element, DateTime? value)
    {
        if (value.HasValue)
        {
            WriteElement(element, value.Value);
        }
    }

    /// <summary>
    /// Writes an element only if the value has a value (for nullable types)
    /// </summary>
    public void WriteElementIfHasValue(string element, decimal? value)
    {
        if (value.HasValue)
        {
            WriteElement(element, value.Value);
        }
    }

    /// <summary>
    /// Writes an element only if the value has a value (for nullable types)
    /// </summary>
    public void WriteElementIfHasValue(string element, double? value)
    {
        if (value.HasValue)
        {
            WriteElement(element, value.Value);
        }
    }

    /// <summary>
    /// Writes an element with an attribute only if the value is not null or empty
    /// </summary>
    public void WriteAttributeElementIfNotEmpty(string element, string attribute, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            WriteAttributeElement(element, attribute, value);
        }
    }
}
