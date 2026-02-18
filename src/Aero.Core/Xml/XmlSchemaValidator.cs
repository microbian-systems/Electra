using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Aero.Common.Xml;

public class XsdSchemaValidator
{
    public List<XmlSchema> Schemas { get; set; }
    public List<String> Errors { get; protected set; }
    public List<String> Warnings { get; protected set; }

    public XsdSchemaValidator()
    {
        Schemas = new List<XmlSchema>();
        Errors = new List<string>();
        Warnings = new List<string>();
    }

    /// <summary>
    /// Add a schema to be used during the validation of the XML document
    /// </summary>
    /// <param name="schemaFileLocation">The file path for the XSD schema file to be added for validation</param>
    /// <returns>True if the schema file was successfully loaded, else false (if false, view Errors/Warnings for reason why)</returns>
    public bool AddSchema(string schemaFileLocation)
    {
        if (String.IsNullOrEmpty(schemaFileLocation)) return false;
        if (!File.Exists(schemaFileLocation)) return false;

        ResetErrorsAndWarnings();

        XmlSchema schema;

        using (var fs = File.OpenRead(schemaFileLocation))
        {
            schema = XmlSchema.Read(fs, ValidationEventHandler);
        }

        var isValid = !Errors.Any() && !Warnings.Any();

        if (isValid)
        {
            Schemas.Add(schema);
        }

        return isValid;
    }

    public async Task<bool> AddSchema(Uri uri)
    {
        ResetErrorsAndWarnings();
        var isValid = false;
        try
        {
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(uri);
                
            var schema = XmlSchema.Read(stream, ValidationEventHandler);
            isValid = !Errors.Any() && !Warnings.Any();

            if (isValid)
                Schemas.Add(schema);
        }
        catch (Exception e)
        {
            Errors.Add(e.Message);
        }

        return isValid;
    }

    protected void ResetErrorsAndWarnings()
    {
        Errors.Clear();
        Warnings.Clear();
    }

    /// <summary>
    /// Perform the XSD validation against the specified XML document
    /// </summary>
    /// <param name="xmlLocation">The full file path of the file to be validated</param>
    /// <returns>True if the XML file conforms to the schemas, else false</returns>
    public bool Validate(string xmlLocation)
    {
        if (!File.Exists(xmlLocation))
            throw new FileNotFoundException("The specified XML file does not exist", xmlLocation);

        using var xmlStream = File.OpenRead(xmlLocation);
        return Validate(xmlStream);
    }

    /// <summary>
    /// Perform XSD validation against the specific XmlDocument object
    /// </summary>
    /// <param name="doc">the XmlDocument object to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public bool Validate(XmlDocument doc)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc), "the XmlDocument to validate cannot be null");

        using var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;
        return Validate(stream);
    }

    /// <summary>
    /// Perform XSD validation against the specific XDocument object
    /// </summary>
    /// <param name="doc">the XDocument object to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public bool Validate(XDocument doc)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc), "the XDocument to validate cannot be null");

        using var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;
        return Validate(stream);
    }

    /// <summary>
    /// Perform XSD validation against an XML string
    /// </summary>
    /// <param name="xmlContent">The XML content to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public bool ValidateFromString(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentNullException(nameof(xmlContent), "XML content cannot be null or empty");

        var bytes = Encoding.UTF8.GetBytes(xmlContent);
        using var stream = new MemoryStream(bytes);
        return Validate(stream);
    }

    /// <summary>
    /// Perform XSD validation asynchronously against the specific XmlDocument object
    /// </summary>
    /// <param name="doc">the XmlDocument object to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public async Task<bool> ValidateAsync(XmlDocument doc)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc), "the XmlDocument to validate cannot be null");

        using var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;
        return await ValidateAsync(stream);
    }

    /// <summary>
    /// Perform XSD validation asynchronously against the specific XDocument object
    /// </summary>
    /// <param name="doc">the XDocument object to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public async Task<bool> ValidateAsync(XDocument doc)
    {
        if (doc == null)
            throw new ArgumentNullException(nameof(doc), "the XDocument to validate cannot be null");

        using var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;
        return await ValidateAsync(stream);
    }

    /// <summary>
    /// Perform XSD validation asynchronously against an XML string
    /// </summary>
    /// <param name="xmlContent">The XML content to validate</param>
    /// <returns>True if validation succeeded false otherwise</returns>
    public async Task<bool> ValidateFromStringAsync(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentNullException(nameof(xmlContent), "XML content cannot be null or empty");

        var bytes = Encoding.UTF8.GetBytes(xmlContent);
        using var stream = new MemoryStream(bytes);
        return await ValidateAsync(stream);
    }

    /// <summary>
    /// Perform the XSD validation against the supplied XML stream
    /// </summary>
    /// <param name="xmlStream">The XML stream to be validated</param>
    /// <returns>True is the XML stream conforms to the schemas, else false</returns>
    public bool Validate(Stream xmlStream)
    {
        // Reset the Error/Warning collections
        Errors = new List<string>();
        Warnings = new List<string>();

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema
        };
        settings.ValidationEventHandler += ValidationEventHandler!;

        foreach (var xmlSchema in Schemas)
        {
            settings.Schemas.Add(xmlSchema);
        }

        if (xmlStream.Position > 0)
            xmlStream.Position = 0;

        using (var xmlFile = XmlReader.Create(xmlStream, settings))
        {
            try
            {
                while (xmlFile.Read()) { }
            }
            catch (XmlException xex)
            {
                Errors.Add(xex.Message);
            }
        }

        return !Errors.Any() && !Warnings.Any();
    }

    /// <summary>
    /// Perform the XSD validation asynchronously against the supplied XML stream
    /// </summary>
    /// <param name="xmlStream">The XML stream to be validated</param>
    /// <returns>True is the XML stream conforms to the schemas, else false</returns>
    public async Task<bool> ValidateAsync(Stream xmlStream)
    {
        // Reset the Error/Warning collections
        Errors = new List<string>();
        Warnings = new List<string>();

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Async = true
        };
        settings.ValidationEventHandler += ValidationEventHandler;

        foreach (var xmlSchema in Schemas)
        {
            settings.Schemas.Add(xmlSchema);
        }

        if (xmlStream.Position > 0)
            xmlStream.Position = 0;

        using (var xmlFile = XmlReader.Create(xmlStream, settings))
        {
            try
            {
                while (await xmlFile.ReadAsync()) { }
            }
            catch (XmlException xex)
            {
                Errors.Add(xex.Message);
            }
        }

        return !Errors.Any() && !Warnings.Any();
    }

    protected virtual void ValidationEventHandler(object sender, ValidationEventArgs e)
    {
        switch (e.Severity)
        {
            case XmlSeverityType.Error:
                Errors.Add(e.Message);
                break;
            case XmlSeverityType.Warning:
                Warnings.Add(e.Message);
                break;
        }
    }
}
