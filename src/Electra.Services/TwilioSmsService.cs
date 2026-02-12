using System.Threading.Tasks;
using Electra.Common;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Electra.Services;

public class TwilioSmsService : ISmsService
{
    private readonly ILogger<TwilioSmsService> log;
    private readonly AppSettings settings;
    private readonly string accountSid;
    private readonly string authToken;

    public TwilioSmsService(AppSettings settings, ILogger<TwilioSmsService> log)
    {
        this.log = log;
        this.settings = settings;
        this.accountSid = settings.Twilio.AccountSid;
        this.authToken = settings.Twilio.AuthToken;
    }

    public async Task SendSms(string from, string to, string body)
    {
        log.LogInformation($"sending twilio sms to {to} with {body}");
        TwilioClient.Init(accountSid, authToken);

        var message = await MessageResource.CreateAsync(
            body: body,
            from: new Twilio.Types.PhoneNumber(from),
            to: new Twilio.Types.PhoneNumber(to)
        );
            
        log.LogInformation($"message: {message}");
    }
}