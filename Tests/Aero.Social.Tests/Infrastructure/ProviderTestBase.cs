using System.Net;
using Aero.Social.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aero.Social.Tests.Infrastructure;

public abstract class ProviderTestBase
{
    protected readonly Mock<ILogger> LoggerMock = new();
    protected readonly MockHttpMessageHandler HttpHandler = new();
    protected readonly Mock<IConfiguration> ConfigurationMock = new();
    protected HttpClient HttpClient => new(HttpHandler);

    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected void SetupConfiguration(string key, string value)
    {
        ConfigurationMock.Setup(x => x[key]).Returns(value);
        ConfigurationMock.Setup(x => x.GetSection(key)).Returns(new Mock<IConfigurationSection>().Object);
    }

    protected void SetupConfigurationSection(string key, Dictionary<string, string> values)
    {
        var sectionMock = new Mock<IConfigurationSection>();
        foreach (var kvp in values)
        {
            sectionMock.Setup(x => x[kvp.Key]).Returns(kvp.Value);
        }
        ConfigurationMock.Setup(x => x.GetSection(key)).Returns(sectionMock.Object);
    }

    protected void VerifyLog(LogLevel level, Times times)
    {
        LoggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    protected static void VerifyLog<T>(Mock<ILogger<T>> loggerMock, LogLevel level, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    protected static void AssertContainsScope(string[] required, string[] granted)
    {
        foreach (var scope in required)
        {
            if (!granted.Contains(scope, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotEnoughScopesException();
            }
        }
    }
}
