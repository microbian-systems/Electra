namespace Aero.Common;

public class SmtpEmailOptions : BaseOptions
{
    public SmtpEmailOptions()
    {
        SectionName = "SmtpEmailOptions";
    }
    public string Host {get; set;}
    public int Port {get; set;}
    public bool EnableSSL {get; set;}
    public string Username {get; set;}
    public string Password {get; set;}
    public string SenderEmail { get; set; }
}