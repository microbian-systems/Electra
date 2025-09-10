using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Electra.Common.Xml;

namespace Electra.Common.Extensions;

public static class XmlDocumentExtension
{
    public static bool ValidateXmlInMemory(this XmlDocument doc, string schemaName = "")
    {
        if (string.IsNullOrEmpty(schemaName))
            throw new ArgumentException($"{nameof(schemaName)} cannot be null/empty");

        var path = Path.GetFullPath(Directory.GetCurrentDirectory() + "\\resources\\" + schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return validator.Validate(doc);
    }

    public static bool ValidateXmlInMemory(this XDocument doc, string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName))
            schemaName = "dfp.xsd";

        var path = Path.GetFullPath(Directory.GetCurrentDirectory() + "\\resources\\" + schemaName);
        var validator = new XsdSchemaValidator();
        validator.AddSchema(path);
        return validator.Validate(doc);
    }

    public static bool ValidateXmlInMemory(this XDocument doc, Uri uri)
    {
        if (uri == null)
            throw new ArgumentNullException($"{nameof(uri)} cannot be null");
            
        var validator = new XsdSchemaValidator();
        validator.AddSchema(uri);
        var res = validator.Validate(doc);
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
            settings.Schemas.Add("http://www.w3.org/2001/XMLSchema", "resources\\" + schemaFileName);
            settings.ValidationType = ValidationType.Schema;

            var reader = string.IsNullOrEmpty(fileName)
                ? XmlReader.Create("document.xml", settings)
                : XmlReader.Create(fileName, settings);

            doc.Load(reader);
            var validationEventHandler = new ValidationEventHandler(ShowCompileErrors);

            doc.Validate(validationEventHandler);

            return ("Completed validating xmlfragment");
        }
        catch (XmlException xmlExp)
        {
            return (xmlExp.Message);
        }
        catch (XmlSchemaException xmlSchExp)
        {
            return (xmlSchExp.Message);
        }
        catch (Exception genExp)
        {
            return (genExp.Message);
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
}