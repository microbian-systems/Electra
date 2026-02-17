namespace Aero.Core.Http;

public abstract class HttpClientBase(HttpClient httpClient, ILogger<HttpClientBase> log)
{
    protected readonly ILogger<HttpClientBase> log = log;
    protected readonly HttpClient httpClient = httpClient;
    protected readonly string jsonMediaType = "application/json";

    protected virtual async Task<HttpResponseMessage> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);

        return response;
    }

    protected virtual async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var serialized = JsonSerializer.Serialize(data);
        request.Content = new StringContent(serialized, Encoding.UTF8, jsonMediaType);
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http POST request for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }

        return response;
    }

    protected virtual Task<HttpResponseMessage> PostAsync<T>(Uri url, T data) 
        where T : class
    {
        return PostAsync(url.ToString(), data);
    }
    
    protected virtual async Task<HttpResponseMessage> PutAsync<T>(string url, T data) 
        where T : class
    {
        var serialized = JsonSerializer.Serialize(data);    
        var content = new StringContent(serialized, Encoding.UTF8, jsonMediaType);
        var response = await httpClient.PutAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http PUT request for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }

        return response;
    }

    protected virtual async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http DELETE request for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }

        return response;
    }

    protected virtual async Task<HttpResponseMessage> PatchAsync<T>(string url, T data)
        where T : class => await PatchAsync(new Uri(url), data);
    
    protected virtual async Task<HttpResponseMessage> PatchAsync<T>(Uri url, T data) where T : class
    {
        var serialized = JsonSerializer.Serialize(data);
        var content = new StringContent(serialized, Encoding.UTF8, jsonMediaType);
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = content
        };
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http PATCH request for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }

        return response;
    }
    
    protected virtual async Task<HttpResponseMessage> OptionAsync(string url)
        => await OptionAsync(new Uri(url));
    
    protected virtual async Task<HttpResponseMessage> OptionAsync(Uri url)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, url);
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http OPTION request for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }
        
        return response;
    }
    
    protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http [{response.RequestMessage.Method}] for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }

        return response;
    }
    
    protected virtual async Task<(T result, HttpResponseMessage response)> SendAsync<T>(HttpRequestMessage request) where T : class
    {
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var ex = new HttpRequestException($"Failed http [{response.RequestMessage.Method}] for {response.RequestMessage.RequestUri}: {response.StatusCode} : {response.ReasonPhrase}");
            log.LogError(ex, ex.Message);
        }
        
        var stream = await response.Content.ReadAsStreamAsync();
        var result = await DeserializeAsync<T>(stream);

        return (result, response);
    }
    
    protected virtual HttpRequestMessage CreateRequest(string url, HttpMethod method) 
        => CreateRequest<object>(url, method, null);
    
    protected virtual HttpRequestMessage CreateRequest(Uri uri, HttpMethod method) 
        => CreateRequest<object>(uri, method, null);

    protected virtual HttpRequestMessage CreateRequest<T>(string url, HttpMethod method, T? data) 
        where T : class => CreateRequest(new Uri(url), method, data);
    
    protected virtual HttpRequestMessage CreateRequest<T>(Uri uri, HttpMethod method, T? data) 
        where T : class
    {
        if (string.IsNullOrEmpty(uri.AbsoluteUri))
            throw new ArgumentNullException(nameof(uri), "Url cannot be null or empty");
        
        if(method is null)
            throw new ArgumentNullException(nameof(method), "HttpMethod cannot be null");
        
        var request = new HttpRequestMessage(method, uri);

        if (data is null) return request;
        
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, jsonMediaType);
        request.Content = content;

        return request;
    }

    protected virtual JsonSerializerOptions GetDefaultSerializerOptions() => 
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    
    protected virtual Task<T> DeserializeAsync<T>(string json) where T : class
        => DeserializeAsync<T>(json, GetDefaultSerializerOptions());
    
    protected virtual async Task<T> DeserializeAsync<T>(string json, JsonSerializerOptions opts) 
        where T : class
    {
        if (string.IsNullOrEmpty(json))
        {
            log.LogWarning("parameter {JsonName} was null or empty. Unable to convert to type {Name}", nameof(json), typeof(T).Name);
            return default!;
        }
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await DeserializeAsync<T>(stream, opts);
    }

    protected virtual async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
        where T : class => await DeserializeAsync<T>(response, GetDefaultSerializerOptions());
    
    protected virtual async Task<T> DeserializeAsync<T>(HttpResponseMessage response, JsonSerializerOptions opts) 
        where T : class
    {
        var str = await response.Content.ReadAsStringAsync();
        return await DeserializeAsync<T>(str, opts);
    }

    protected virtual async Task<T> DeserializeAsync<T>(Stream stream)
        where T : class => await DeserializeAsync<T>(stream, GetDefaultSerializerOptions());
    
    protected virtual async Task<T> DeserializeAsync<T>(Stream stream, JsonSerializerOptions opts) 
        where T : class
    {
        var result = await JsonSerializer.DeserializeAsync<T>(stream, opts);
        return result!;
    }
}