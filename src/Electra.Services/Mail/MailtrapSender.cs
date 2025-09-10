using System.Net.Mail;
using System.Threading;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using FluentEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Electra.Services.Mail;

public static class FluentEmailMailtrapBuilderExtensions
{
    public static FluentEmailServicesBuilder AddMailtrapSender(this FluentEmailServicesBuilder builder)
    {
        var sp = builder.Services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptionsMonitor<SmtpEmailOptions>>().CurrentValue;
            
        return AddMailtrapSender(builder, opts.Username, opts.Password, opts.Host, opts.Port);
    }
        
    public static FluentEmailServicesBuilder AddMailtrapSender(this FluentEmailServicesBuilder builder, string userName, string password, string host = null, int? port = null)
    {
        builder.Services.TryAdd(ServiceDescriptor.Scoped<ISender>(x => new MailtrapSender(userName, password, host, port)));
        return builder;
    }
}
    
/// <summary>
/// Send emails to a Mailtrap.io inbox
/// </summary>
public class MailtrapSender : ISender
{
    private readonly SmtpClient _smtpClient;
    private static readonly int[] ValidPorts = {25, 465, 2525};

    /// <summary>
    /// Creates a sender that uses the given Mailtrap credentials, but does not dispose it.
    /// </summary>
    /// <param name="userName">Username of your mailtrap.io SMTP inbox</param>
    /// <param name="password">Password of your mailtrap.io SMTP inbox</param>
    /// <param name="host">Host address for the Mailtrap.io SMTP inbox</param>
    /// <param name="port">Port for the Mailtrap.io SMTP server. Accepted values are 25, 465 or 2525.</param>
    public MailtrapSender(string userName, string password, string host = "smtp.mailtrap.io", int? port = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Mailtrap UserName needs to be supplied", nameof(userName));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Mailtrap Password needs to be supplied", nameof(password));

        if (port.HasValue && !ValidPorts.Contains(port.Value))
            throw new ArgumentException("Mailtrap Port needs to be either 25, 465 or 2525", nameof(port));
            
        _smtpClient = new SmtpClient(host, port.GetValueOrDefault(2525))
        {
            Credentials = new NetworkCredential(userName, password),
            EnableSsl = true
        };
    }
        
    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        var smtpSender = new SmtpSender(_smtpClient);
        return smtpSender.Send(email, token);
    }

    public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
    {
        var smtpSender = new SmtpSender(_smtpClient);
        return await smtpSender.SendAsync(email, token);
    }
}