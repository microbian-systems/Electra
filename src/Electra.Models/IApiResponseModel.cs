using System.Net;

namespace Electra.Models;


public interface IWebResponseModel 
{
    HttpStatusCode StatusCode { get; set; }
    string ReasonPhrase { get; set; }
    bool IsSuccessStatusCode { get; set; }
}

public interface IWebResponseModel<T> : IWebResponseModel
{
    T Data { get; set; }
}

public interface IWebResponseCollectionModel<T> : IWebResponseModel<List<T>>
{
}
