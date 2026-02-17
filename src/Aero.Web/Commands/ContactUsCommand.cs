using Aero.Persistence.Entities;
using Aero.Models;
using Aero.Common.Commands;
using Aero.Marten;
using Microsoft.AspNetCore.Identity.UI.Services;
using ILogger = Serilog.ILogger;
using WebResponse = Aero.Models.WebResponse;

namespace Aero.Common.Web.Commands;

public class ContactUsCommand : IAsyncCommand<ContactUsModel, WebResponse>
{
    private readonly ILogger log;
    private readonly IDynamicMartenRepository db;
    private readonly IEmailSender emailer;

    public ContactUsCommand(IDynamicMartenRepository db, IEmailSender emailer, ILogger log)
    {
        this.log = log;
        this.db = db;
        this.emailer = emailer;
    }
        
    public async Task<WebResponse> ExecuteAsync(ContactUsModel model)
    {
        log.Information($"contact-us message recieved from: {model.Name} - {model.Email}");
        // todo - write failed messages to msg queue

        var contactMessage = new ContactMessage()
        {
            Name = model.Name,
            Email = model.Email,
            Message = model.Message
        };

        //await db.SaveAsync(contactMessage);
            
        await emailer.SendEmailAsync(model.Email, model.Name, model.Message);
        log.Information($"contact-us message sent");
            
        return new WebResponse()
        {
            Message = $"successfully sent email to {model.Email}"
        };
    }
}
    
//   todo - add ip checking middleware to prevent spammers / bots etc...
//    const string cachekey = "contact-us-dict";
//    Func<string, object> getDict = (key) => (object)new Dictionary<string, int> {{ip, 1}};
//    cache.TryGetOrAdd(cachekey, getDict, out object attempts);
//    var entry = (Dictionary<string, int>)attempts;
//    entry[ip] += 1;
//    if(entry[ip] > 5)
//    return new BadRequestResult();
//            
//    cache.AddOrUpdate(cachekey, attempts, o => o);