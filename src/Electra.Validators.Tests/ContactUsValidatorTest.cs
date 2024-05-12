using System.Threading.Tasks;
using Electra.Models;
using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Electra.Validators.Tests
{
    public class ContactUsValidatorTest
    {
        private static readonly IMemoryCache cache = A.Fake<IMemoryCache>();
        private static readonly ILogger<ContactUsValidator> log = A.Fake<ILogger<ContactUsValidator>>();
        private readonly ContactUsValidator validator = new ContactUsValidator(cache, log);
      
        [Fact]
        public async Task ContactUs_Validator_Test_Valid()
        {
            var model = new ContactUsModel()
            {
                Email = "test@test.com",
                Message =  "a message",
                Name = "me"
            };
            var result = await validator.ValidateAsync(model);
            
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ContactUs_Validator_Test_Invalid()
        {
            var model = new ContactUsModel();
            var result = await validator.ValidateAsync(model);
            
            Assert.False(result.IsValid);           
        }
    }
}