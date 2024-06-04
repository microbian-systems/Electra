using System.Threading.Tasks;
using Electra.Common.Xml;

namespace Electra.Common.Tests;

public class ElectraXmlWriterTests
{
    private readonly Faker faker = new();

    [Fact]
    public void WriteAttributeElement_ShouldWriteElementWithNamespaceAttributeAndValue()
    {
        var element = faker.Random.Word();
        var ns = faker.Internet.Url();
        var attribute = faker.Random.Word();
        var value = faker.Random.Word();

        var writer = new ElectraXmlWriter();
        writer.WriteAttributeElement(element, ns, attribute, value);

        var result = writer.GetXmlAsString();
        result.Should().Be($"<{element} {attribute}=\"{value}\" xmlns=\"{ns}\" />");
    }

    [Fact]
    public void WriteAttributeElement_ShouldWriteElementWithAttributeAndValue()
    {
        var element = faker.Random.Word();
        var attribute = faker.Random.Word();
        var value = faker.Random.Word();

        var writer = new ElectraXmlWriter();
        writer.WriteAttributeElement(element, attribute, value);

        var result = writer.GetXmlAsString();
        result.Should().Be($"<{element} {attribute}=\"{value}\" />");
    }

    [Fact]
    public async Task WriteAttributeElement_ShouldWriteElementWithNamespaceAttributeAndValueAsync()
    {
        var element = faker.Random.Word();
        var ns = faker.Internet.Url();
        var attribute = faker.Random.Word();
        var value = faker.Random.Word();

        var writer = new ElectraXmlWriter();
        writer.WriteAttributeElement(element, ns, attribute, value);

        var result = await writer.GetXmlAsStringAsync();
        result.Should().Be($"<{element} {attribute}=\"{value}\" xmlns=\"{ns}\" />");
    }

    [Fact]
    public async Task WriteAttributeElement_ShouldWriteElementWithAttributeAndValueAsync()
    {
        var element = faker.Random.Word();
        var attribute = faker.Random.Word();
        var value = faker.Random.Word();

        var writer = new ElectraXmlWriter();
        writer.WriteAttributeElement(element, attribute, value);

        var result = await writer.GetXmlAsStringAsync();
        result.Should().Be($"<{element} {attribute}=\"{value}\" />");
    }
}