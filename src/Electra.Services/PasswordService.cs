using PasswordGenerator;

namespace Microbians.Services
{
    public interface IPasswordService
    {
        string GeneratePassword(int length = 12);
        string GenerateOneTimePass(int length = 5);
    }

    public class PasswordService : IPasswordService
    {
        public string GeneratePassword(int length = 12)
        {
            var password = new Password(length)
                .IncludeNumeric()
                .IncludeLowercase()
                .IncludeUppercase()
                .IncludeSpecial()
                .LengthRequired(length);
            
            return password.Next();
        }

        public string GenerateOneTimePass(int length = 5)
        {
            var password = new Password(length).IncludeNumeric();
            return password.Next();
        }
    }
}