using System.Net.Http;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Electra.Services.Geo;

public interface IZipApiService
{
    Task<(bool success, string city, string state, string state_fullname)> GetCityAndState(string zip);

    /// <summary>
    ///
    /// </summary>
    /// <param name="city"></param>
    /// <param name="state">full state name, no abbreviations</param>
    /// <returns></returns>
    Task<(bool status, string[] data)> GetZipCodesByCityAndState(string city, string state);

    Task<(bool status, List<(string zipCode, string distance)>)> GetRadius(string zip, int radius);
    Task<(bool status, (string distance, string unit) data)> GetDistance(string zip1, string zip2, string unit = "mi");
    Task<(bool status, (string zipcode, string[] county) data)> GetCounty(string zip);
}

// todo - use https://www.smartystreets.com/pricing when demand grows or https://loqate.com/
public class ZipApiService : IZipApiService
{
    private readonly ILogger<ZipApiService> log;
    private readonly ZipApiSettings settings;
    private readonly HttpClient client;
    private readonly string apiKey;
    private const string param = "X-API-KEY=";

    public ZipApiService(ZipApiSettings zipApiSettings, AppSettings appSettings, ILogger<ZipApiService> log)
    {
        this.log = log;
        this.settings = zipApiSettings;

        this.apiKey = settings.ApiKey;
        var url = new Uri(this.settings.Url);
        var credentials = new NetworkCredential(settings.Username, settings.Password);
        var httpClientHandler = new HttpClientHandler()
        {
            Credentials = credentials
        };

        client = new HttpClient(httpClientHandler)
        {
            BaseAddress = url
        };
    }

    public async Task<(bool success, string city, string state, string state_fullname)> GetCityAndState(string zip)
    {
        log.LogInformation($"getting city and state for zip code: {zip}");

        var json = await client.GetStringAsync($"zipcode/{zip}/?{param}{apiKey}");
        //var result = JsonSerializer.Deserialize<(bool status, (string city, string state, string state_fullname, string latitude, string longitude) data)>(json);
        //dynamic res1 = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
        var res = JsonSerializer.Deserialize<ZipResponse>(json);


        if (res.Status)
            //return (true, res.Data.city, res.Data.state, res.Data.state_fullname);
            return (true, res.Data.City, res.Data.State, res.Data.StateFullName);

        return (false, "", "", "");
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="city"></param>
    /// <param name="state">full state name, no abbreviations</param>
    /// <returns></returns>
    public async Task<(bool status, string[] data)> GetZipCodesByCityAndState(string city, string state)
    {
        log.LogInformation($"getting zip codes for city {city} and state {state}");

        var json = await client.GetStringAsync($"zipcode/zips/?{param}{apiKey}&city={city}&state={state}");
        var res = JsonSerializer.Deserialize<dynamic>(json);

        if (res.success)
            return (true, res.data);

        return (false, Array.Empty<string>());
    }

    public async Task<(bool status, List<(string zipCode, string distance)>)> GetRadius(string zip, int radius)
    {
        log.LogInformation($"getting radius for zip code {zip}");

        var json = await client.GetStringAsync($"zipcode/radius/{zip}?{param}{apiKey}&radius={radius}");
        var res = JsonSerializer.Deserialize<dynamic>(json);

        if (res.success)
            return (true, res.data);

        return (false, new List<(string zipCode, string distance)>());
    }

    public async Task<(bool status, (string distance, string unit) data)> GetDistance(string zip1, string zip2,
        string unit = "mi")
    {
        log.LogInformation($"getting distance bewteen {zip1} and {zip2}");

        var json = await client.GetStringAsync(
            $"zipcode/distance/?{param}{apiKey}&zip1={zip1}&zip2={zip2}&unit={unit}");
        var res = JsonSerializer.Deserialize<dynamic>(json);

        if (res.success)
            return (true, res.data);

        return (false, ("", ""));
    }

    public async Task<(bool status, (string zipcode, string[] county) data)> GetCounty(string zip)
    {
        log.LogInformation($"getting county for zip {zip}");

        var json = await client.GetStringAsync($"zipcode/county/{zip}/?{param}{apiKey}");
        var res = JsonSerializer.Deserialize<dynamic>(json);

        if (res.success)
            return (true, res.data);

        return (false, ("", Array.Empty<string>()));
    }
}

public record ZipResponse
{
    [JsonPropertyName("status")] public bool Status { get; set; }

    [JsonPropertyName("data")] public DataObj Data { get; set; }
}

public record DataObj
{
    [JsonPropertyName("city")] public string City { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("state_fullname")] public string StateFullName { get; set; }
}