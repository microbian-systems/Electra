namespace Electra.Services
{
    public interface ISmsService
    {
        Task SendSms(string from, string to, string body);
    }
}