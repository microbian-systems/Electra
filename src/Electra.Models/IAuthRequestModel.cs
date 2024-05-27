namespace Electra.Models;

public interface IAuthRequestModel
{
    [JsonPropertyName("id")]
    string Id { get; init; }
}

public interface IApiKeyAuthRequestModel : IAuthRequestModel
{
    [JsonPropertyName("api_key")]
    string ApiKey
    {
        get => Id;
        init => Id = value;
    }
}

public interface IBasicAuthRequestModel : IAuthRequestModel
{
    [JsonPropertyName("username")]
    public string Username
    {
        get => Id;
        init => Id = value;
    }
    
    [JsonPropertyName("password")]
    public string Password { get; init; }
}