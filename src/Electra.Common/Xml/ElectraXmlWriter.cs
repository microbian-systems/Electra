using System.Xml;

namespace Electra.Common.Xml;

public class ElectraXmlWriter
{
    private readonly XmlTextWriter xmlTextWriter;
    private readonly StringBuilder stringBuilder;

    public ElectraXmlWriter()
    {
        stringBuilder = new StringBuilder();
        xmlTextWriter = new XmlTextWriter(new StringWriter(stringBuilder));
    }

    public void WriteAttributeElement(string element, string ns, string attribute, string value)
    {
        xmlTextWriter.WriteStartElement(element, ns);
        xmlTextWriter.WriteAttributeString(attribute, value);
        xmlTextWriter.WriteEndElement();
    }

    public void WriteAttributeElement(string element, string attribute, string value)
    {
        xmlTextWriter.WriteStartElement(element);
        xmlTextWriter.WriteAttributeString(attribute, value);
        xmlTextWriter.WriteEndElement();
    }

    public string GetXmlAsString()
    {
        xmlTextWriter.Flush();
        return stringBuilder.ToString();
    }

    public Task<string> GetXmlAsStringAsync()
    {
        xmlTextWriter.Flush();
        return Task.FromResult(stringBuilder.ToString());
    }

    public void Dispose()
    {
        xmlTextWriter.Close();
    }
}