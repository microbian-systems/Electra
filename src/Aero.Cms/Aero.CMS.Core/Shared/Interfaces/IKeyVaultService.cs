namespace Aero.CMS.Core.Shared.Interfaces;

public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
}
