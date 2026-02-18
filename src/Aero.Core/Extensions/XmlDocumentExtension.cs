using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using Aero.Common.Xml;

namespace Aero.Common.Extensions;

public static class XmlDocumentExtension
{
    #region Validation Methods

    public static bool ValidateXmlInMemory(this XmlDocument doc, string schemaName = "")
    {
        if (string.IsNullOrEmpty(schemaName))
            throw new ArgumentException($"{nameof(schemaName)} cannot be null/empty");

        var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return validator.Validate(doc);
    }

    public static bool ValidateXmlInMemory(this XDocument doc, string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName))
            throw new ArgumentException($"{nameof(schemaName)} cannot be null/empty");

        var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return validator.Validate(doc);
    }

    public static bool ValidateXmlInMemory(this XDocument doc, Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));
            
        var validator = new XsdSchemaValidator();
        // Use GetAwaiter().GetResult() for synchronous execution - only use for simple scenarios
        validator.AddSchema(uri).GetAwaiter().GetResult();
        var res = validator.Validate(doc);
        if (!res)
            throw new Exception(string.Join(Environment.NewLine, validator.Errors));
        return res;
    }

    public static async Task<bool> ValidateXmlInMemoryAsync(this XmlDocument doc, string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName))
            throw new ArgumentException($"{nameof(schemaName)} cannot be null/empty");

        var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return await validator.ValidateAsync(doc);
    }

    public static async Task<bool> ValidateXmlInMemoryAsync(this XDocument doc, string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName))
            throw new ArgumentException($"{nameof(schemaName)} cannot be null/empty");

        var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return await validator.ValidateAsync(doc);
    }

    public static async Task<bool> ValidateXmlInMemoryAsync(this XDocument doc, Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));
            
        var validator = new XsdSchemaValidator();
        await validator.AddSchema(uri);
        var res = await validator.ValidateAsync(doc);
        if (!res)
            throw new Exception(string.Join(Environment.NewLine, validator.Errors));
        return res;
    }

    public static string ValidateXml(this XmlDocument doc, string fileName = "", string schema = "")
    {
        try
        {
            var settings = new XmlReaderSettings();
            var schemaFileName = schema;
            settings.Schemas.Add("http://www.w3.org/2001/XMLSchema", Path.Combine("resources", schemaFileName));
            settings.ValidationType = ValidationType.Schema;

            using (var reader = string.IsNullOrEmpty(fileName)
                ? XmlReader.Create("document.xml", settings)
                : XmlReader.Create(fileName, settings))
            {
                doc.Load(reader);
            }
            
            var validationEventHandler = new ValidationEventHandler(ShowCompileErrors);
            doc.Validate(validationEventHandler);

            return "Completed validating xmlfragment";
        }
        catch (XmlException xmlExp)
        {
            return xmlExp.Message;
        }
        catch (XmlSchemaException xmlSchExp)
        {
            return xmlSchExp.Message;
        }
        catch (Exception genExp)
        {
            return genExp.Message;
        }
    }

    #endregion

    #region Conversion Methods

    /// <summary>
    /// Converts an XmlDocument to an XDocument
    /// </summary>
    public static XDocument ToXDocument(this XmlDocument xmlDocument)
    {
        if (xmlDocument == null)
            throw new ArgumentNullException(nameof(xmlDocument));

        using (var reader = new XmlNodeReader(xmlDocument))
        {
            reader.MoveToContent();
            return XDocument.Load(reader);
        }
    }

    /// <summary>
    /// Converts an XDocument to an XmlDocument
    /// </summary>
    public static XmlDocument ToXmlDocument(this XDocument xDocument)
    {
        if (xDocument == null)
            throw new ArgumentNullException(nameof(xDocument));

        var xmlDocument = new XmlDocument();
        using (var reader = xDocument.CreateReader())
        {
            xmlDocument.Load(reader);
        }
        return xmlDocument;
    }

    /// <summary>
    /// Converts an XmlElement to an XElement
    /// </summary>
    public static XElement ToXElement(this XmlElement xmlElement)
    {
        if (xmlElement == null)
            throw new ArgumentNullException(nameof(xmlElement));

        return XElement.Parse(xmlElement.OuterXml);
    }

    /// <summary>
    /// Converts an XElement to an XmlElement
    /// </summary>
    public static XmlElement ToXmlElement(this XElement xElement)
    {
        if (xElement == null)
            throw new ArgumentNullException(nameof(xElement));

        var xmlDoc = new XmlDocument();
        using (var reader = xElement.CreateReader())
        {
            xmlDoc.Load(reader);
        }
        return xmlDoc.DocumentElement;
    }

    #endregion

    #region Serialization Methods

    /// <summary>
    /// Serializes an object to an XmlDocument
    /// </summary>
    public static XmlDocument SerializeToXmlDocument<T>(this T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var serializer = new XmlSerializer(typeof(T));
        var xmlDoc = new XmlDocument();
        
        using (var stream = new MemoryStream())
        {
            serializer.Serialize(stream, obj);
            stream.Position = 0;
            xmlDoc.Load(stream);
        }
        
        return xmlDoc;
    }

    /// <summary>
    /// Serializes an object to an XDocument
    /// </summary>
    public static XDocument SerializeToXDocument<T>(this T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var serializer = new XmlSerializer(typeof(T));
        
        using (var stream = new MemoryStream())
        {
            serializer.Serialize(stream, obj);
            stream.Position = 0;
            return XDocument.Load(stream);
        }
    }

    /// <summary>
    /// Serializes an object to an XML string
    /// </summary>
    public static string SerializeToXmlString<T>(this T obj, bool indent = true)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Indent = indent,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };

        using (var stream = new StringWriter())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            serializer.Serialize(writer, obj);
            return stream.ToString();
        }
    }

    /// <summary>
    /// Deserializes an XmlDocument to an object
    /// </summary>
    public static T DeserializeFromXmlDocument<T>(this XmlDocument xmlDocument)
    {
        if (xmlDocument == null)
            throw new ArgumentNullException(nameof(xmlDocument));

        var serializer = new XmlSerializer(typeof(T));
        using (var reader = new XmlNodeReader(xmlDocument))
        {
            return (T)serializer.Deserialize(reader);
        }
    }

    /// <summary>
    /// Deserializes an XDocument to an object
    /// </summary>
    public static T DeserializeFromXDocument<T>(this XDocument xDocument)
    {
        if (xDocument == null)
            throw new ArgumentNullException(nameof(xDocument));

        var serializer = new XmlSerializer(typeof(T));
        using (var reader = xDocument.CreateReader())
        {
            return (T)serializer.Deserialize(reader);
        }
    }

    /// <summary>
    /// Deserializes an XML string to an object
    /// </summary>
    public static T DeserializeFromXmlString<T>(this string xmlString)
    {
        if (string.IsNullOrWhiteSpace(xmlString))
            throw new ArgumentNullException(nameof(xmlString));

        var serializer = new XmlSerializer(typeof(T));
        using (var reader = new StringReader(xmlString))
        {
            return (T)serializer.Deserialize(reader);
        }
    }

    #endregion

    #region Formatting Methods

    /// <summary>
    /// Formats an XmlDocument with proper indentation
    /// </summary>
    public static string ToFormattedString(this XmlDocument xmlDocument, bool omitXmlDeclaration = false)
    {
        if (xmlDocument == null)
            throw new ArgumentNullException(nameof(xmlDocument));

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = omitXmlDeclaration
        };

        using (var stream = new StringWriter())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            xmlDocument.Save(writer);
            return stream.ToString();
        }
    }

    /// <summary>
    /// Formats an XDocument with proper indentation
    /// </summary>
    public static string ToFormattedString(this XDocument xDocument, bool omitXmlDeclaration = false)
    {
        if (xDocument == null)
            throw new ArgumentNullException(nameof(xDocument));

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = omitXmlDeclaration
        };

        using (var stream = new StringWriter())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            xDocument.Save(writer);
            return stream.ToString();
        }
    }

    /// <summary>
    /// Minifies an XML string by removing unnecessary whitespace
    /// </summary>
    public static string MinifyXml(this string xmlString)
    {
        if (string.IsNullOrWhiteSpace(xmlString))
            return xmlString;

        var doc = new XmlDocument();
        doc.LoadXml(xmlString);
        
        RemoveWhitespaceNodes(doc.DocumentElement);
        
        var settings = new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = false
        };

        using (var stream = new StringWriter())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            doc.Save(writer);
            return stream.ToString();
        }
    }

    private static void RemoveWhitespaceNodes(XmlNode node)
    {
        if (node == null) return;

        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
            var child = node.ChildNodes[i];
            if (child.NodeType == XmlNodeType.Whitespace || child.NodeType == XmlNodeType.SignificantWhitespace)
            {
                node.RemoveChild(child);
            }
            else if (child.HasChildNodes)
            {
                RemoveWhitespaceNodes(child);
            }
        }
    }

    #endregion

    #region XPath Methods

    /// <summary>
    /// Selects a single node using XPath and returns it as an XElement
    /// </summary>
    public static XElement SelectElement(this XDocument doc, string xpath)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc));
        if (string.IsNullOrWhiteSpace(xpath))
            throw new ArgumentException($"{nameof(xpath)} cannot be null or empty", nameof(xpath));

        var navigator = doc.CreateNavigator();
        var node = navigator.SelectSingleNode(xpath);
        
        if (node?.UnderlyingObject is XElement element)
            return element;
        
        return null;
    }

    /// <summary>
    /// Selects nodes using XPath and returns them as XElements
    /// </summary>
    public static IEnumerable<XElement> SelectElements(this XDocument doc, string xpath)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc));
        if (string.IsNullOrWhiteSpace(xpath))
            throw new ArgumentException($"{nameof(xpath)} cannot be null or empty", nameof(xpath));

        var navigator = doc.CreateNavigator();
        var iterator = navigator.Select(xpath);
        var elements = new List<XElement>();

        while (iterator.MoveNext())
        {
            if (iterator.Current?.UnderlyingObject is XElement element)
                elements.Add(element);
        }

        return elements;
    }

    /// <summary>
    /// Gets the value of an attribute safely
    /// </summary>
    public static string GetAttributeValue(this XElement element, string attributeName, string defaultValue = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrWhiteSpace(attributeName))
            throw new ArgumentException($"{nameof(attributeName)} cannot be null or empty", nameof(attributeName));

        var attribute = element.Attribute(attributeName);
        return attribute?.Value ?? defaultValue;
    }

    /// <summary>
    /// Gets the value of a child element safely
    /// </summary>
    public static string GetElementValue(this XElement parent, string elementName, string defaultValue = null)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));
        if (string.IsNullOrWhiteSpace(elementName))
            throw new ArgumentException($"{nameof(elementName)} cannot be null or empty", nameof(elementName));

        var element = parent.Element(elementName);
        return element?.Value ?? defaultValue;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if an XML string is well-formed
    /// </summary>
    public static bool IsWellFormedXml(this string xmlString)
    {
        if (string.IsNullOrWhiteSpace(xmlString))
            return false;

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlString);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Safely loads an XML file into an XDocument with error handling
    /// </summary>
    public static XDocument SafeLoadXDocument(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("XML file not found", filePath);

        try
        {
            return XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Failed to load XML file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Safely loads an XML file into an XmlDocument with error handling
    /// </summary>
    public static XmlDocument SafeLoadXmlDocument(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("XML file not found", filePath);

        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);
            return doc;
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Failed to load XML file: {ex.Message}", ex);
        }
    }

    public static void ShowCompileErrors(object sender, ValidationEventArgs args)
    {
        switch (args.Severity)
        {
            case XmlSeverityType.Error:
                Console.WriteLine("Validation Error: {0}", args.Message);
                break;
            case XmlSeverityType.Warning:
                Console.WriteLine("Validation Warning {0}", args.Message);
                break;
        }
    }

    #endregion
}
