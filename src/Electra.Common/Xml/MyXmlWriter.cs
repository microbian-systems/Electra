using System.IO;
using System.Text;
using System.Xml;

namespace Microbians.Common.Xml
{
    public class MyXmlWriter : XmlTextWriter
    {
            public MyXmlWriter(Stream w, Encoding encoding) : base(w, encoding) { }
            public MyXmlWriter(string filename, Encoding encoding) : base(filename, encoding) { }

            public void WriteAttributeElement(string element, string ns, string attribute, string value)
            {
                this.WriteStartElement(element, ns);
                this.WriteAttributeString(attribute, value);
                this.WriteEndElement();
            }

            public void WriteAttributeElement(string element, string attribute, string value)
            {
                this.WriteStartElement(element);
                this.WriteAttributeString(attribute, value);
                this.WriteEndElement();
            }

    }
}
