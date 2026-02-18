namespace Aero.Social.Models;

public class ClientInformation
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string InstanceUrl { get; set; } = string.Empty;
}

public class FetchPageInformationResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class MentionResult
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public bool DoNotCache { get; set; }
}

public class NoMentionResult
{
    public bool None => true;
}
