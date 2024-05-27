using System.Net.Http;
using Electra.Models;

namespace Electra.Common.Web.Extensions;

public static class WebResponseExtensions
{
    public static WebResponseModel<T> As<T>(this IWebResponseModel model)
        where T : class => (WebResponseModel<T>)model;

    public static Task<WebResponseModel> ToWebResponseModel(this HttpResponseMessage response)
    {
        var webResponse = new WebResponseModel
        {
            StatusCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
        };

        return Task.FromResult(webResponse);
    }

    public static async Task<IWebResponseModel<T>> ToWebResponseModel<T>(this HttpResponseMessage response)
        where T : class
    {
        return new WebResponseModel<T>
        {
            StatusCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            Data = (await response.DeserializeContent<T>())!,
        };
    }


    public static async Task<IWebResponseCollectionModel<TType>> ToWebResponseCollectionModel<TType>
        (this HttpResponseMessage response)
    {
        return new WebResponseCollectionModel<TType>
        {
            StatusCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            Data = (await response.DeserializeContent<List<TType>>())!,
        };
    }

    private static async Task<T?> DeserializeContent<T>(this HttpResponseMessage response)
        where T : class
    {
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        var model = JsonSerializer.Deserialize<T>(json);

        return model ?? null;
    }
}