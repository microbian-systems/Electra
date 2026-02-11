using Electra.Core.Configuration;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Electra.SendGrid.Tests;

public class SendGridTests
{
    private readonly IConfiguration config;
    private readonly ILogger log;

    public SendGridTests()
    {
        config = ConfigHelper.GetConfigurationRoot();
        this.log = A.Fake<ILogger>();
    }
}