namespace Microbians.Common.Constants
{
    public static class RegExConstants
    {
        // todo -  verify email regex pattern is valid for newer TLD's
        // https://stackoverflow.com/questions/46155/how-to-validate-an-email-address-in-javascript
        public const string Email = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
        
        // todo - verify phone regex pattern is valid for all phone numbers in US
        public const string Phone = @"\(?\d{3}\)?[. -]? *\d{3}[. -]? *[. -]?\d{4}";
    }
}