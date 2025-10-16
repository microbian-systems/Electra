using ThrowGuard;

namespace Electra.Social.Forem;

public class ForemApiKeyHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // add stuff here - logging possibly
        
        return await base.SendAsync(request, cancellationToken);
    }
}