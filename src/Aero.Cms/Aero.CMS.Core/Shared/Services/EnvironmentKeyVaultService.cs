using Aero.CMS.Core.Shared.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Aero.CMS.Core.Shared.Services;

public class EnvironmentKeyVaultService(IConfiguration configuration) : IKeyVaultService
{
    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
        => Task.FromResult(configuration[key]);
}
