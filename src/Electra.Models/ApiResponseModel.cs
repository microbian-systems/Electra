using System.Net;

namespace Electra.Models;


public record WebResponseDynamicModel : WebResponseModel<dynamic>
{
}

public record WebResponseObjectModel : WebResponseModel<object>
{
}

public record WebResponseCollectionModel<T> : WebResponseModel<List<T>>, IWebResponseCollectionModel<T>
{
}

public record WebResponseModel<T> : WebResponseModel, IWebResponseModel<T>
{
    public virtual T Data { get; set; } = default!;
}

public record WebResponseModel : IWebResponseModel
{
    public HttpStatusCode StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public bool IsSuccessStatusCode { get; set; }
}
