using Electra.Common.Extensions;

namespace Electra.Common.Tests;

public class Base64ExtensionsTests
{
    [Fact]
    public void Base64Decode_ShouldReturnDecodedString_WhenEncodedStringIsProvided()
    {
        var encodedString = "SGVsbG8gd29ybGQ=";
        var decodedString = encodedString.Base64Decode();
        decodedString.Should().Be("Hello world");
    }

    [Fact]
    public void Base64Encode_ShouldReturnEncodedString_WhenDecodedStringIsProvided()
    {
        var decodedString = "Hello world";
        var encodedString = decodedString.Base64Encode();
        encodedString.Should().Be("SGVsbG8gd29ybGQ=");
    }

    [Fact]
    public void Base64Encode_ShouldReturnEncodedString_WhenByteArrayIsProvided()
    {
        var byteArray = new byte[] { 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 }; // Represents the string "Hello world"
        var encodedString = byteArray.Base64Encode();
        encodedString.Should().Be("SGVsbG8gd29ybGQ=");
    }
}