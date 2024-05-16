namespace Electra.Common.Web.Infrastructure;

public interface IClientsService
{
    Task<Dictionary<string, Guid>> GetActiveClients();
    Task InvalidateApiKey(string apiKey);
}

public class ClientsService : IClientsService
{
    public async Task<Dictionary<string, Guid>> GetActiveClients()
    {
        throw new NotImplementedException();
    }

    public async Task InvalidateApiKey(string apiKey)
    {
        throw new NotImplementedException();
    }
}