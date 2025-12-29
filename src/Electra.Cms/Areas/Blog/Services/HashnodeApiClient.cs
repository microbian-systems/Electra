

namespace Electra.Web.BlogEngine.Services;

public class HashnodeApiClient : IHashnodeApiClient
{
    public Task GetPostBySlugAsync(string slug)
    {
        return Task.CompletedTask;
    }
}

public interface IHashnodeApiClient

{
}
