using Microbians.Core.Configuration;
using FakeItEasy;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microbians.SendGrid.Tests
{

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
}